#!/usr/bin/env bash

# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

# Fail on any failed commands
set -e

GIT_DIR="$(git rev-parse --show-toplevel)"
DIST="$GIT_DIR/dist"

# Clean Up existing dist.
rm -rf "$DIST"

# This block stolen from our src/install.sh
#------------------------------------------
os_info="$(uname -a)"
os_name="$(echo $os_info | cut -d ' ' -f1)"
case "${os_name}" in
    Darwin)
        name="${name}-osx"
        arch="$(echo $os_info | rev | cut -d ' ' -f1 | rev)"

        case "${arch}" in
            arm64)
                runtime="osx-arm64"
                ;;
            x86_64)
                runtime="osx-x64"
                ;;
            *)
                error "Unsupported architecture '${arch}'"
                exit 1
                ;;
        esac
        ;;
    *)
        error "Unsupported OS '${os_name}'"
        exit 1
        ;;
esac
#------------------------------------------

# Publish
dotnet publish "$GIT_DIR/src/AzureAuth/AzureAuth.csproj" --self-contained true -r "$runtime" -c release -o $DIST $*
