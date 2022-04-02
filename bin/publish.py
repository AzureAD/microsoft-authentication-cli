# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

from typing import List
from subprocess import run
from sys import stdout, stderr, exit, argv
from os import environ, sep as dirsep, path
from versioning import get_version, print_header

BUILD_SOURCES_DIRECTORY = 'BUILD_SOURCES_DIRECTORY'
BUILD_REASON = 'BUILD_REASON'
BUILD_CONFIG = "BUILD_CONFIG"

use_debug = any([
    BUILD_REASON in environ and environ[BUILD_REASON] == 'PullRequest',
    BUILD_CONFIG in environ and environ[BUILD_CONFIG] == 'debug',
])

BUILD_CONFIG = "debug" if use_debug else "release"

WIN10_RID = "win10-x64"
OSX_RID = "osx-x64"

USAGE = f"USAGE:\n{argv[0]} PROJECT RUNTIME"

DOTNET_PUBLISH = ["dotnet", "publish"]
DOTNET_PUBLISH_OPTIONS = [
    "-c", BUILD_CONFIG,
    "--force",
    f"-p:Version={get_version()}",
    "--self-contained",
    # "-p:PublishTrimmed=true",
]

WIN_OPTIONS = [
    "-r", WIN10_RID,
]

OSX_OPTIONS = [
    "-r", OSX_RID,
]


def output_win(project: str) -> List[str]:
    return ["-o", path.join(project, 'dist', WIN10_RID)]


def output_osx(project: str) -> List[str]:
    return ["-o", path.join(project, 'dist', OSX_RID)]


def shell_run(command: List[str]) -> None:
    print_header(f"\nRunning: {' '.join(command)}")
    result = run(command, stdout=stdout, stderr=stderr)
    if result.returncode != 0:
        print("\n" + ' '.join(result.stderr), flush=True)
        raise Exception(f"Failed to run {command}")


def csproj_path(name: str) -> str:
    prefix = '.'
    if BUILD_SOURCES_DIRECTORY in environ:
        prefix = environ[BUILD_SOURCES_DIRECTORY]

    return path.join(prefix, name, f"{name}.csproj")


def publish_project(project_name: str, runtime: str):
    csproj = csproj_path(project_name)
    command = DOTNET_PUBLISH + [csproj] + DOTNET_PUBLISH_OPTIONS

    print_header(f"\nPublishing '{project_name}'")

    try:
        if runtime == 'win10-x64':
            shell_run(command + WIN_OPTIONS + output_win(project_name))
        elif runtime == 'osx-x64':
            shell_run(command + OSX_OPTIONS + output_osx(project_name))
        else:
            print(f"Unkown runtime '{runtime}'")

    except Exception as e:
        print("Oops, we hit a snag publishing...", flush=True)
        exit(1)


def main():
    if len(argv) <= 2:
        print(USAGE)
        exit(0)

    publish_project(argv[1], argv[2])


if __name__ == "__main__":
    main()
