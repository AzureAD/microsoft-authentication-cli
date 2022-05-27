"""A script which validates strings as mostly SemVer compliant. We add a 'v'."""

import re
import sys

# Strictly speaking, this isn't exactly SemVer because we've added a 'v' to the
# prefix, but everything else is taken from https://semver.org.
#
# See also:
# - https://semver.org/#is-v123-a-semantic-version
# - https://semver.org/#is-there-a-suggested-regular-expression-regex-to-check-a-semver-string
MOSTLY_SEMVER = re.compile(r"^v(?P<major>0|[1-9]\d*)\.(?P<minor>0|[1-9]\d*)\.(?P<patch>0|[1-9]\d*)(?:-(?P<prerelease>(?:0|[1-9]\d*|\d*[a-zA-Z-][0-9a-zA-Z-]*)(?:\.(?:0|[1-9]\d*|\d*[a-zA-Z-][0-9a-zA-Z-]*))*))?(?:\+(?P<buildmetadata>[0-9a-zA-Z-]+(?:\.[0-9a-zA-Z-]+)*))?$")

def main() -> None:
    """Validate user input."""
    version = input("Enter a version: ")
    match MOSTLY_SEMVER.match(version):
        case None:
            sys.exit(f"Invalid version: {version}")
        case _:
            print(f"Congrats, {version} is valid!")


if __name__ == "__main__":
    main()
