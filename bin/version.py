"""A script which validates strings as SemVer compliant."""

import re
import sys

# See this link for more info:
# - https://semver.org/#is-there-a-suggested-regular-expression-regex-to-check-a-semver-string
SEMVER = re.compile(r"^(?P<major>0|[1-9]\d*)\.(?P<minor>0|[1-9]\d*)\.(?P<patch>0|[1-9]\d*)(?:-(?P<prerelease>(?:0|[1-9]\d*|\d*[a-zA-Z-][0-9a-zA-Z-]*)(?:\.(?:0|[1-9]\d*|\d*[a-zA-Z-][0-9a-zA-Z-]*))*))?(?:\+(?P<buildmetadata>[0-9a-zA-Z-]+(?:\.[0-9a-zA-Z-]+)*))?$")

def main() -> None:
    """Validate user input."""
    version = input("Enter a version: ")
    match SEMVER.match(version):
        case None:
            sys.exit(f"Invalid version: {version}")
        case _:
            print(f"Congrats, {version} is valid!")


if __name__ == "__main__":
    main()
