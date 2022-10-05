"""A script which triggers and wait for an azure devops to complete a release"""
from argparse import ArgumentDefaultsHelpFormatter, ArgumentParser
from ast import parse
import os
import sys
import time
from azure.devops.connection import Connection
from msrest.authentication import BasicAuthentication


def wait_for_release(release_client, project, release_id, timeout, interval=30):
    release = release_client.get_release(project, release_id)
    start = time.start()
    while not has_release_completed(release) and time.time() - start < timeout:
        time.sleep(interval)
        release = release_client.get_release(project, release_id)
    return release


def has_release_completed(release):
    status = release.environments[0].status
    return status in ("succeeded", "canceled", "partiallySucceeded", "rejected")


def has_release_failed(release):
    status = release.environments[0].status
    return status in ("canceled", "partiallySucceeded", "rejected")


def create_azure_devops_release(organization, project, timeout, ADO_PAT):
    """"""
    connection = create_ado_connection(organization, ADO_PAT)

    release_client = connection.clients.get_release_client()

    azureauth_definition_id = get_ado_azureauth_release_definition_id(
        project, release_client
    )

    release_metadata = {
        "definitionId": azureauth_definition_id,
    }

    create_release_response = release_client.create_release(release_metadata, "OE")
    release = wait_for_release("OE", create_release_response.id)

    return release


def get_ado_azureauth_release_definition_id(project, release_client):
    """"""
    definitions = release_client.get_release_definitions(project)

    azureauth_definition_id = None
    for definition in definitions.value:
        if definition.name == "AzureAuth Linux":
            azureauth_definition_id = definition.id
            break
    # TODO : handle definition not found/multiple definitions found
    return azureauth_definition_id


def create_ado_connection(organization, ADO_PAT) -> Connection:
    """Returns an ADO connection to call the ADO Rest APIs."""

    organization_url = "https://dev.azure.com/{0}".format(organization)
    credentials = BasicAuthentication("", ADO_PAT)
    connection = Connection(base_url=organization_url, creds=credentials)

    # TODO : handle connection failures/exceptions
    return connection


def main() -> None:
    # 1. Parse command line arguments.
    parser = ArgumentParser(
        description=__doc__,
        formatter_class=ArgumentDefaultsHelpFormatter,
    )

    parser.add_argument(
        "--organization",
        help="The name of the Azure DevOps organization",
        default="office",
    )

    parser.add_argument(
        "--project", help="The name/ID of the Azure DevOps project", default="OE"
    )

    parser.add_argument(
        "--timeout",
        help="azure devops release timeout in seconds (default: 3600)",
        default=3600,
    )

    args = parser.parse_args()

    # 2. Read env vars.
    try:
        # ADO PAT (Azure Devops Personal Access Token) with "Release" scope.
        # More information here - https://learn.microsoft.com/en-us/azure/devops/organizations/accounts/use-personal-access-tokens-to-authenticate?view=azure-devops&tabs=Windows#create-a-pat
        ADO_PAT = os.environ["AZURE_DEVOPS_RELEASE_PAT"]
    except KeyError as exc:
        # See https://stackoverflow.com/a/24999035/3288364.
        name = str(exc).replace("'", "")
        sys.exit(f"Error: missing env var: {name}")

    # 3. Create and wait for the azure devops release to be finished/timed out.
    try:
        release = create_azure_devops_release(
            args.organization, args.project, args.timeout, ADO_PAT
        )
    except Exception as e:
        print(e)
        # TODO

    # 4. Exit based on the release status
    if has_release_failed(release):
        release_url = "https://dev.azure.com/{0}/{1}/_releaseProgress?_a=release-pipeline-progress&releaseId={2}".format(
            args.organization, args.project, release.id
        )
        sys.exit(
            "More details on triggered pipeline can be found here: {0}".format(
                release_url
            )
        )
