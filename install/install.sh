#!/bin/sh

set -e

# We use an env var for this rather than --verbose because getopt is not reliably cross-platform.
: ${AZUREAUTH_VERBOSE_INSTALL=""}
verbose() {
    if [ -n "${AZUREAUTH_VERBOSE_INSTALL}" ]; then
        >&2 printf "\x1b[33m${1}\x1b[0m\n"
    fi
}
error() { >&2 printf "\x1b[31m${1}\x1b[0m\n"; }

version="${AZUREAUTH_VERSION}"
if [ -z "${version}" ]; then
    error 'No $AZUREAUTH_VERSION specified, unable to download a release'
    exit 1
fi

# Parse the OS info from uname into a proper release artifact name.
release_name() {
    name="azureauth-${version}"
    os_info="$(uname -a)"
    os_name="$(echo $os_info | cut -d ' ' -f1)"

    case "${os_name}" in
        Darwin)
            name="${name}-osx"
            arch="$(echo $os_info | rev | cut -d ' ' -f1 | rev)"

            case "${arch}" in
                arm64)
                    name="${name}-arm64"
                    ;;
                x86_64)
                    name="${name}-x64"
                    ;;
                *)
                    error "Unsupported architecture '${arch}', unable to download a release"
                    exit 1
                    ;;
            esac
            ;;
        *)
            error "Unsupported OS '${os_name}', unable to download a release"
            exit 1
            ;;
    esac

    echo "${name}"
}

remove_from_profile() {
    path="${1}"
    shell_profile="${2}"

    if grep "${path}" "${shell_profile}" &>/dev/null; then
        # Output which path we're removing for debugging purposes.
        path_suffix=$(echo "${path}" | awk -F : '{print $NF}' | tr -d '"')
        verbose "Removing '${path_suffix}' from \$PATH in ${shell_profile}"

        # Escape the current path so that / are replaced with \/ and the string
        # can be given to sed as a valid expression.
        escaped_path=$(echo "${path}" | sed -e 's;/;\\/;g')
        # Delete a matching path (including trailing newline) from the profile.
        sed -i -e /"${escaped_path}"/d "${shell_profile}"
    fi
}

install_pre_0_4_0() {
    : ${AZUREAUTH_REPO='AzureAD/microsoft-authentication-cli'}
    repo="${AZUREAUTH_REPO}"
    release_file="$(release_name).tar.gz"
    release_url="https://github.com/${repo}/releases/download/${version}/${release_file}"

    : ${AZUREAUTH_INSTALL_DIRECTORY="${HOME}/.azureauth"}
    azureauth_directory="${AZUREAUTH_INSTALL_DIRECTORY}"
    target_directory="${azureauth_directory}/$(release_name)"
    latest_directory="${azureauth_directory}/latest"
    tarball="${azureauth_directory}/${release_file}"

    verbose "Installing using pre-0.4.0 method"

    verbose "Creating ${azureauth_directory}"
    mkdir -p $azureauth_directory

    verbose "Downloading ${release_url} to ${tarball}"
    if ! curl -sfL $release_url > $tarball; then
        error "Failed to download azureauth ${version}"
        exit 1
    fi

    if [ -d "${target_directory}" ]; then
        verbose "Removing pre-existing target directory at ${target_directory}"
        rm -rf $target_directory
    fi

    verbose "Extracting ${tarball} to ${target_directory}"
    mkdir -p $target_directory
    tar -xf $tarball -C $target_directory

    # The files extracted from the tarball are all executable, but only two need to be.
    chmod -x ${target_directory}/*
    chmod +x ${target_directory}/azureauth

    verbose "Removing ${tarball}"
    rm $tarball

    verbose "Linking ${latest_directory} to ${target_directory}"
    # It's very important we use -n and -f here or the symlink won't actually be overwritten during upgrades.
    ln -snf $target_directory $latest_directory

    # We currently only support automatically appending $PATH modifications for the default
    # Bash and ZSH profiles as the syntax is identical.
    path_modification='export PATH="${PATH}:${HOME}/.azureauth/latest"'
    for shell_profile in "${HOME}/.bashrc" "${HOME}/.zshrc"
    do
        if ! grep "${path_modification}" "${shell_profile}" &>/dev/null; then
            verbose "Updating \$PATH in ${shell_profile} to include ${latest_directory}"
            printf "\n${path_modification}\n" >> $shell_profile
        fi
    done

    echo "Installed azureauth ${version}!"
}

install_post_0_4_0() {
    : ${AZUREAUTH_REPO='AzureAD/microsoft-authentication-cli'}
    repo="${AZUREAUTH_REPO}"
    release_file="$(release_name).tar.gz"
    release_url="https://github.com/${repo}/releases/download/${version}/${release_file}"

    : ${AZUREAUTH_INSTALL_DIRECTORY="${HOME}/.azureauth"}
    azureauth_directory="${AZUREAUTH_INSTALL_DIRECTORY}"
    target_directory="${azureauth_directory}/${version}"
    tarball="${azureauth_directory}/${release_file}"

    # Ignore profile updates if the user has requested we not touch their profile(s).
    : ${AZUREAUTH_NO_UPDATE_PATH=""}
    no_update_path="${AZUREAUTH_NO_UPDATE_PATH}"

    verbose "Installing using post-0.4.0 method"

    verbose "Creating ${azureauth_directory}"
    mkdir -p $azureauth_directory

    verbose "Downloading ${release_url} to ${tarball}"
    if ! curl -sfL $release_url > $tarball; then
        error "Failed to download azureauth ${version}"
        exit 1
    fi

    if [ -d "${target_directory}" ]; then
        verbose "Removing pre-existing target directory at ${target_directory}"
        rm -rf $target_directory
    fi

    verbose "Extracting ${tarball} to ${target_directory}"
    mkdir -p $target_directory
    tar -xf $tarball -C $target_directory

    verbose "Removing ${tarball}"
    rm $tarball

    if [ -n "${no_update_path}" ]; then
        verbose "Not updating the \$PATH in any user profiles"
    else
        # We previously added `latest` to the $PATH, but we no longer support that, so
        # we remove this from the $PATH to avoid confusion.
        latest_path='export PATH="${PATH}:${HOME}/.azureauth/latest"'

        # If there is an existing installation we can identify, then we remove it from
        # the $PATH so that it will be replaced by the new installation. Note that we use
        # `true` here because we set -e above and this expression would fail and cause
        # unnecessary early termination otherwise.
        current_azureauth="$(which azureauth || true)"
        if [ -f "${current_azureauth}" ]; then
            current_azureauth_parent=$(dirname "${current_azureauth}")
            current_path='export PATH="${PATH}:'${current_azureauth_parent}'"'
        fi

        for shell_profile in "${HOME}/.bashrc" "${HOME}/.zshrc"
        do
            remove_from_profile "${latest_path}" "${shell_profile}"
            if [ -n "${current_path}" ]; then
                remove_from_profile "${current_path}" "${shell_profile}"
            fi
        done

        # We currently only support automatically appending $PATH modifications for the default
        # Bash and ZSH profiles as the syntax is identical.
        new_path='export PATH="${PATH}:'${target_directory}'"'
        for shell_profile in "${HOME}/.bashrc" "${HOME}/.zshrc"
        do
            if ! grep "${new_path}" "${shell_profile}" &>/dev/null; then
                verbose "Appending '${target_directory}' to \$PATH in ${shell_profile}"
                printf "${new_path}\n" >> $shell_profile
            fi
        done
    fi

    echo "Installed azureauth ${version}!"
}

case "${version}" in
    v0.1.0|v0.2.0|v0.3.0|0.3.1)
        install_pre_0_4_0
        ;;
    *)
        install_post_0_4_0
        ;;
esac
