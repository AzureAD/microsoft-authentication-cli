# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

"""A script which triggers and waits for Azure DevOps to complete a build"""

import os
import sys
import time

from azure.devops.connection import Connection
from azure.devops.released.build.build_client import BuildClient
from azure.devops.v5_1.build.models import Build
from msrest.authentication import BasicAuthentication

# https://learn.microsoft.com/en-us/rest/api/azure/devops/build/builds/get?view=azure-devops-rest-7.1#buildresult
FAILED_STATUSES: set[str] = {"canceled", "partiallySucceeded", "failed"}
COMPLETED_STATUSES: set[str] = FAILED_STATUSES | {"succeeded"}


def ado_connection(organization: str, ado_pat: str) -> Connection:
    """Returns an ADO connection to call the ADO REST APIs."""

    return Connection(
        base_url=f"https://dev.azure.com/{organization}",
        creds=BasicAuthentication("", ado_pat),
    )


def wait_for_build(build_client: BuildClient, project: str, build_id: str) -> Build:
    """Wait for the azure devops build to finish"""
    build = build_client.get_build(project, build_id)

    # polling interval is set in accordance with the rate limits specified here:
    # https://learn.microsoft.com/en-us/azure/devops/integrate/concepts/rate-limits?view=azure-devops
    polling_interval_seconds = 30

    # Wait until the build have a complete status.
    while build.result not in COMPLETED_STATUSES:
        time.sleep(polling_interval_seconds)
        build = build_client.get_build(project, build_id)
    return build


def trigger_azure_pipeline_and_wait_until_its_completed(
    organization: str,
    project: str,
    pipeline_id: str,
    ado_pat: str,
) -> None:
    """Triggers an azure pipeline and waits for it to be finished"""
    build_client = ado_connection(organization, ado_pat).clients.get_build_client()
    build_metadata = {"definition": {"id": pipeline_id}}

    triggered_build = build_client.queue_build(build_metadata, project)
    build_url = f"https://dev.azure.com/{organization}/{project}/_build/results?buildId={triggered_build.id}&view=results"
    print(
        "Successfully triggered a build. Waiting for the build to be completed.\n"
        f"More details on the build can be found here: {build_url}"
    )

    completed_build = wait_for_build(build_client, project, triggered_build.id)
    if completed_build.result in FAILED_STATUSES:
        raise Exception("Azure DevOps build failed!")

    print("Azure DevOps build succeeded!")


def main() -> None:
    # 1. Read env vars.
    try:
        # ADO PAT (Azure DevOps Personal Access Token) with "Build" scope.
        # More information here - https://learn.microsoft.com/en-us/azure/devops/organizations/accounts/use-personal-access-tokens-to-authenticate?view=azure-devops&tabs=Windows#create-a-pat
        ado_pat = os.environ["AZURE_DEVOPS_BUILD_PAT"]
        organization = os.environ["ADO_ORGANIZATION"]
        project = os.environ["ADO_PROJECT"]
        pipeline_id = os.environ["ADO_AZUREAUTH_LINUX_PIPELINE_ID"]

    except KeyError as exc:
        # See https://stackoverflow.com/a/24999035/3288364.
        name = str(exc).replace("'", "")
        sys.exit(f"Error: missing env var: {name}")

    # 2. Trigger azure pipeline and wait for it to be finished.
    trigger_azure_pipeline_and_wait_until_its_completed(
        organization,
        project,
        pipeline_id,
        ado_pat,
    )


if __name__ == "__main__":
    main()
