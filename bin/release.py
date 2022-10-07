# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

"""A script which triggers and waits for Azure DevOps to complete a release"""

import os
import sys
import time
from ast import parse

from azure.devops.connection import Connection
from msrest.authentication import BasicAuthentication

# https://learn.microsoft.com/en-us/rest/api/azure/devops/release/releases/get-release?view=azure-devops-rest-6.0&tabs=HTTP#environmentstatus
FAILED_STATUSES: set[str] = {"canceled", "partiallySucceeded", "rejected"}
COMPLETED_STATUSES: set[str] = FAILED_STATUSES | {"succeeded"}


def create_ado_connection(organization, ado_pat) -> Connection:
    """Returns an ADO connection to call the ADO REST APIs."""

    return Connection(
        base_url=f"https://dev.azure.com/{organization}",
        creds=BasicAuthentication("", ado_pat),
    )


def get_release_definition(project, pipeline_name, release_client):
    """Returns the ADO definition for the given pipeline"""
    # Get all release definitions in a given project
    project_release_definitions = release_client.get_release_definitions(project)

    # Filter release definitions with the given pipeline name
    pipeline_release_definitions = [
        definition
        for definition in project_release_definitions.value
        if definition.name == pipeline_name
    ]

    if not pipeline_release_definitions:
        raise Exception(
            f"Pipeline named {pipeline_name} not found in project {project}"
        )

    if len(pipeline_release_definitions) > 1:
        raise Exception(
            f"More than 1 Pipeline named {pipeline_name} found in project {project}"
        )

    return pipeline_release_definitions[0]


def populate_release_metadata(project, pipeline_name, release_client):
    """Returns release metadata with required information"""
    release_definition = get_release_definition(project, pipeline_name, release_client)
    return {"definitionId": release_definition.id}


def release_status_match(release, expected_status_list):
    """Returns True if status of all the environments of the release match any of the expected statuses"""
    # Each release can have one or more environments (stages).
    release_env_statuses = [environment.status for environment in release.environments]
    return all(status in expected_status_list for status in release_env_statuses)


def wait_for_release(release_client, project, release_id):
    """Wait for the azure devops release to finish"""
    release = release_client.get_release(project, release_id)

    # polling interval is set in accordance with the rate limits specified here:
    # https://learn.microsoft.com/en-us/azure/devops/integrate/concepts/rate-limits?view=azure-devops
    polling_interval_seconds = 30

    # Wait until the release have a complete status.
    while not release_status_match(release, COMPLETED_STATUSES):
        time.sleep(polling_interval_seconds)
        release = release_client.get_release(project, release_id)
    return release


def create_and_wait_for_azure_devops_release(
    organization, project, pipeline_name, ADO_PAT
):
    """Creates and waits for an azure devops release to be finished"""
    connection = create_ado_connection(organization, ADO_PAT)
    release_client = connection.clients.get_release_client()
    release_metadata = populate_release_metadata(project, pipeline_name, release_client)

    triggered_release = release_client.create_release(release_metadata, project)
    release_url = f"https://dev.azure.com/{organization}/{project}/_releaseProgress?_a=release-pipeline-progress&releaseId={triggered_release.id}"
    print(
        "Successfully triggered a release. Waiting for the release to be completed.\n"
        f"More details on the release can be found here: {release_url}"
    )

    completed_release = wait_for_release(release_client, project, triggered_release.id)
    if release_status_match(completed_release, FAILED_STATUSES):
        raise Exception("Azure DevOps release failed!")

    print("Azure DevOps release succeeded!")


def main() -> None:
    # 1. Read env vars.
    try:
        # ADO PAT (Azure DevOps Personal Access Token) with "Release" scope.
        # More information here - https://learn.microsoft.com/en-us/azure/devops/organizations/accounts/use-personal-access-tokens-to-authenticate?view=azure-devops&tabs=Windows#create-a-pat
        ado_pat = os.environ["AZURE_DEVOPS_RELEASE_PAT"]
    except KeyError as exc:
        # See https://stackoverflow.com/a/24999035/3288364.
        name = str(exc).replace("'", "")
        sys.exit(f"Error: missing env var: {name}")

    # 2. Create and wait for the azure devops release to be finished.
    organization = "office"
    project = "OE"
    pipeline_name = "AzureAuth Linux"
    create_and_wait_for_azure_devops_release(
        organization,
        project,
        pipeline_name,
        ado_pat,
    )


if __name__ == "__main__":
    main()
