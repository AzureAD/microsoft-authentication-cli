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

# Detect architecture (x64 or arm64) based on current system
detect_arch() {
    arch="$(uname -m)"

    case "${arch}" in
        x86_64|amd64)
            echo "x64"
            ;;
        aarch64|arm64)
            echo "arm64"
            ;;
        *)
            error "Unsupported architecture '${arch}', unable to download a release"
            exit 1
            ;;
    esac
}

# Parse the OS info from uname into a proper release artifact name.
release_name() {
    name="azureauth-${version}"
    os_name="$(uname -s)"

    case "${os_name}" in
        Linux)
            arch="$(detect_arch)"
            name="${name}-linux-${arch}"
            ;;
        *)
            error "Unsupported OS '${os_name}', unable to download a release"
            exit 1
            ;;
    esac

    echo "${name}"
}

install_post_0_4_0() {
    : ${AZUREAUTH_REPO='AzureAD/microsoft-authentication-cli'}
    repo="${AZUREAUTH_REPO}"
    release_file="$(release_name).deb"
    release_url="https://github.com/${repo}/releases/download/${version}/${release_file}"

    # Download location for the .deb file (not the installation location)
    # The actual installation location is determined by the .deb package (/usr/bin/azureauth)
    : ${AZUREAUTH_DOWNLOAD_DIRECTORY="/tmp"}
    download_directory="${AZUREAUTH_DOWNLOAD_DIRECTORY}"
    deb_file="${download_directory}/${release_file}"

    verbose "Installing using Debian package method"
    verbose "Detected architecture: $(detect_arch)"

    verbose "Creating download directory ${download_directory}"
    mkdir -p "${download_directory}"

    verbose "Downloading ${release_url} to ${deb_file}"
    if ! curl -sfL "${release_url}" > "${deb_file}"; then
        error "Failed to download azureauth ${version}"
        exit 1
    fi

    verbose "Installing ${deb_file} using dpkg"
    if ! sudo dpkg -i "${deb_file}"; then
        error "Failed to install azureauth ${version}"
        error "You may need to run 'sudo apt-get install -f' to fix dependencies"
        exit 1
    fi

    verbose "Removing ${deb_file}"
    rm "${deb_file}"

    echo "Installed azureauth ${version}!"
}

case "${version}" in
    v0.1.0|v0.2.0|v0.3.0|0.3.1)
        error "Version ${version} does not have Linux .deb packages available"
        exit 1
        ;;
    *)
        install_post_0_4_0
        ;;
esac
