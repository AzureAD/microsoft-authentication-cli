from json import loads
from datetime import datetime
from subprocess import run
from sys import exit, argv


def get_token(azureauth, resource, client, tenant):
    result = run(
        [
            azureauth,
            "--output",
            "json",
            "--resource",
            resource,
            "--client",
            client,
            "--tenant",
            tenant,
        ],
        capture_output=True,
    )
    if result.returncode != 0:
        print(result.stderr)
        exit(result.returncode)

    token_result = loads(result.stdout)
    token_result['expiration_date'] = datetime.fromtimestamp(int(token_result['expiration_date']))
    return token_result


if __name__ == "__main__":
    if len(argv) < 2:
        print(f"USAGE: {argv[0]} PATH_TO_AZUREAUTH_CLI\n(On Windows run publish.cmd to build the application and see it's output path.")
        exit(1)

    azureauth = argv[1].strip()
    resource = input("Enter a resource ID: ")
    client = input("Enter a client ID: ")
    tenant = input("Enter a tenant ID: ")

    t = get_token(azureauth, resource, client, tenant)
    print(f"Token for {t['user']} ({t['display_name']} valid until {t['expiration_date']})")
