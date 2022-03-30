# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

import sys
import os
import shutil
from subprocess import run
from versioning import get_version, print_header

WIN_RID = "win10-x64"
OSX_RID = "osx-x64"


def generate_nuspec(nuspec: str, gen_nuspec: str, id: str, rid: str) -> None:
    with open(nuspec, 'r', encoding='utf-8') as in_f:
        nuspec_content = in_f.read()

    nuspec_content = nuspec_content \
        .replace('<id></id>', f"<id>{id}</id>") \
        .replace('<!--insert-dist-->', f'<file src="dist\\{rid}\\" target="dist\\{rid}\\" />')

    print(f"Generating nuspec to use at '{gen_nuspec}'", flush=True)
    with open(gen_nuspec, 'w', encoding='utf-8') as out_f:
        out_f.write(nuspec_content)


def package_up(project: str, nuspec: str, package_name: str, rid: str) -> int:
    id = f"{package_name}.{rid}"
    version = get_version()
    print_header(f"\nPackaging {id} @ {version}")

    gen_nuspec = os.path.join(project, f"{project}.gen.{rid}.nuspec")
    generate_nuspec(nuspec, gen_nuspec, id, rid)
    result = run(["nuget", "pack", gen_nuspec, "-NoPackageAnalysis", "-Version", version],
                 stdout=sys.stdout, stderr=sys.stderr)

    os.remove(gen_nuspec)

    return result.returncode == 0


def main():
    if len(sys.argv) < 4:
        print(
            f"Error: Usage: {sys.argv[0]} CSPROJ_FOLDER PACKAGE_NAME_BASE RUNTIME")
        sys.exit(1)

    project = sys.argv[1].strip()
    package_name = sys.argv[2].strip()
    runtime = sys.argv[3].strip()

    nuspec = os.path.join(project, f"{project}.template.nuspec")

    if package_up(project, nuspec, package_name, runtime):
        return 0
    else:
        return 1


if __name__ == "__main__":
    exit(main())
