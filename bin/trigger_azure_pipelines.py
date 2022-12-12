# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

"""A script which triggers and waits for Azure DevOps to complete a build"""

import os
import sys
import time

from azure.devops.connection import Connection
from azure.devops.v6_0.pipelines.pipelines_client import PipelinesClient
from azure.devops.v6_0.pipelines.models import Run
from msrest.authentication import BasicAuthentication

# https://learn.microsoft.com/en-us/rest/api/azure/devops/pipelines/runs/get?view=azure-devops-rest-6.0#runresult
FAILED_STATUSES: set[str] = {"canceled", "failed", "unknown"}


def ado_connection(organization: str, ado_pat: str) -> Connection:
    """Returns an ADO connection to call the ADO REST APIs."""

    return Connection(
        base_url=f"https://dev.azure.com/{organization}",
        creds=BasicAuthentication("", ado_pat),
    )


def wait_for_pipeline_run(pipeline_client: PipelinesClient, project: str, pipeline_id: int, run_id: str) -> Run:
    """Wait for the azure devops pipepline run to finish"""
    run = pipeline_client.get_run(project, pipeline_id, run_id)

    # polling interval is set in accordance with the rate limits specified here:
    # https://learn.microsoft.com/en-us/azure/devops/integrate/concepts/rate-limits?view=azure-devops
    polling_interval_seconds = 30

    # Wait until the build have a complete status.
    # https://learn.microsoft.com/en-us/rest/api/azure/devops/pipelines/runs/get?view=azure-devops-rest-6.0#runstate
    while run.state != "completed":
        time.sleep(polling_interval_seconds)
        run = pipeline_client.get_run(project, pipeline_id, run_id)
    return run


def trigger_azure_pipeline_and_wait_until_its_completed(
    organization: str,
    project: str,
    pipeline_id: int,
    ado_pat: str,
    version: str,
    commitSHA: str
) -> None:
    """Triggers an azure pipeline and waits for it to be finished"""
    ado_client = ado_connection(organization, ado_pat).clients_v6_0
    pipeline_client = ado_client.get_pipelines_client()
    
    # NOTE: We run a pipeline instead of queuing a build because only the run pipeline API allows us to pass template parameters and build API doesn't support it.
    # run pipeline API: https://learn.microsoft.com/en-us/rest/api/azure/devops/pipelines/runs/run-pipeline?view=azure-devops-rest-6.0 
    # queue build API: https://learn.microsoft.com/en-us/rest/api/azure/devops/build/builds/queue?view=azure-devops-rest-6.0

    # run_parameters = {"templateParameters": {"version": version, "commitSHA": commitSHA}}
    run_parameters = {"templateParameters": {"upstream_version": version, "commitSHA": commitSHA}, 
        "resources": {"repositories": {"self": {"refName": "refs/heads/debian_scripts"}}}}

    pipeline_status = pipeline_client.run_pipeline(run_parameters, project, pipeline_id)
    pipeline_url = f"https://dev.azure.com/{organization}/{project}/_build/results?buildId={pipeline_status.id}&view=results"
    print(
        "Successfully triggered a pipeline. Waiting for the run to be completed.\n"
        f"More details on the triggered pipeline can be found here: {pipeline_url}"
    )
    completed_run = wait_for_pipeline_run(pipeline_client, project, pipeline_id, pipeline_status.id)
    if completed_run.result in FAILED_STATUSES:
        raise Exception("Azure DevOps pipeline run failed!")


def main() -> None:
    # 1. Read env vars.
    try:
        # ADO PAT (Azure DevOps Personal Access Token) with "Build" scope.
        # More information here - https://learn.microsoft.com/en-us/azure/devops/organizations/accounts/use-personal-access-tokens-to-authenticate?view=azure-devops&tabs=Windows#create-a-pat
        ado_pat = os.environ["AZURE_DEVOPS_BUILD_PAT"]
        organization = os.environ["ADO_ORGANIZATION"]
        project = os.environ["ADO_PROJECT"]
        pipeline_id = os.environ["ADO_AZUREAUTH_LINUX_PIPELINE_ID"]
        version = os.environ["VERSION"]
        commitSHA = os.environ["commitSHA"]
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
        version,
        commitSHA
    )


if __name__ == "__main__":
    main()
