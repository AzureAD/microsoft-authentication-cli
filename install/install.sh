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

: ${AZUREAUTH_REPO='AzureAD/microsoft-authentication-cli'}
repo="${AZUREAUTH_REPO}"
release_file="$(release_name).tar.gz"
release_url="https://github.com/${repo}/releases/download/${version}/${release_file}"

azureauth_directory="${HOME}/.azureauth"
target_directory="${azureauth_directory}/${version}"
latest_directory="${azureauth_directory}/latest"
tarball="${azureauth_directory}/${release_file}"

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
ln -sf $target_directory $latest_directory

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
