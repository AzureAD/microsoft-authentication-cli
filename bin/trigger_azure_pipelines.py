# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

"""A script which triggers and waits for Azure DevOps to complete a build"""
import io
import os
import sys
import time
import zipfile

from azure.devops.connection import Connection
from azure.devops.v6_0.pipelines.pipelines_client import PipelinesClient
from azure.devops.v6_0.build.build_client import BuildClient
from azure.devops.v6_0.build.models import TimelineRecord
from msrest.authentication import BasicAuthentication
from requests import Response

# https://learn.microsoft.com/en-us/rest/api/azure/devops/build/timeline/get#taskresult
FAILED_RESULTS: set[str] = {"abandoned", "canceled", "failed", "skipped"}

# https://learn.microsoft.com/en-us/rest/api/azure/devops/build/timeline/get#timelinerecordstate
COMPLETED_STATES: set[str] = {"completed"}


def ado_connection(organization: str, ado_pat: str) -> Connection:
    """Returns an ADO connection to call the ADO REST APIs."""

    return Connection(
        base_url=f"https://dev.azure.com/{organization}",
        creds=BasicAuthentication("", ado_pat),
    )

def wait_for_stage(
    build_client: BuildClient, project: str, stage_id: str, run_id: str
) -> TimelineRecord:
    """Wait for the dedicated stage in azure devops pipepline run to finish"""

    # polling interval is set in accordance with the rate limits specified here:
    # https://learn.microsoft.com/en-us/azure/devops/integrate/concepts/rate-limits?view=azure-devops
    polling_interval_seconds = 30

    # Wait until the stage have a complete status.
    # https://learn.microsoft.com/en-us/rest/api/azure/devops/build/timeline/get?view=azure-devops-rest-6.0
    while True:
        timeline = build_client.get_build_timeline(project, run_id)
        record = next((r for r in timeline.records if r.identifier == stage_id), None)
        if record == None or record.state not in COMPLETED_STATES:
            time.sleep(polling_interval_seconds)
        else:
            return record


def trigger_azure_pipeline_and_wait_until_its_completed(
    ado_client: Connection,
    organization: str,
    project: str,
    pipeline_id: str,
    stage_id: str,
    version: str,
    debian_revision: str,
    commit_hash: str,
) -> str:
    """Triggers an azure pipeline and waits for it to be finished"""
    pipeline_client = ado_client.get_pipelines_client()

    # NOTE: We run a pipeline instead of queuing a build because only the run pipeline API allows us to pass template parameters and build API doesn't support it.
    # run pipeline API: https://learn.microsoft.com/en-us/rest/api/azure/devops/pipelines/runs/run-pipeline?view=azure-devops-rest-6.0
    # queue build API: https://learn.microsoft.com/en-us/rest/api/azure/devops/build/builds/queue?view=azure-devops-rest-6.0

    run_parameters = {
        "templateParameters": {"upstream_version": version, "commit_hash": commit_hash, "debian_revision": debian_revision}
    }
    pipeline_status = pipeline_client.run_pipeline(run_parameters, project, pipeline_id)
    run_id = pipeline_status.id
    pipeline_url = f"https://dev.azure.com/{organization}/{project}/_build/results?buildId={run_id}&view=results"
    print(
        "Successfully triggered a pipeline. Waiting for the run to be completed.\n"
        f"More details on the triggered pipeline can be found here: {pipeline_url}"
    )
    completed_run = wait_for_stage(
        ado_client.get_build_client(), project, stage_id, pipeline_status.id
    )

    if completed_run.result in FAILED_RESULTS:
        raise Exception("Azure DevOps pipeline run failed!")

    return run_id


def download_callback(chunk: bytes, response: Response) -> None:
    print(f"Downloaded chunk of size: {len(chunk)}")


def download_artifact(
    ado_client: Connection,
    project: str,
    run_id: str,
    ado_artifact_name: str,
    download_path: str,
) -> None:
    """Download the ADO artifact to the given download path"""
    build_client = ado_client.get_build_client()
    artifact = build_client.get_artifact_content_zip(
        project,
        run_id,
        ado_artifact_name,
        download=True,
        callback=download_callback,
    )

    # Read the stream of bytes to a bytearray.
    # The bytestream is a zip file.
    # And then extract the zip contents to given path.
    content = bytearray()
    for chunk in artifact:
        content += bytearray(chunk)
    zf = zipfile.ZipFile(io.BytesIO(content), "r")
    zf.extractall(download_path)


def main() -> None:
    # 1. Read env vars.
    try:
        # ADO PAT (Azure DevOps Personal Access Token) with "Build" scope.
        # More information here - https://learn.microsoft.com/en-us/azure/devops/organizations/accounts/use-personal-access-tokens-to-authenticate?view=azure-devops&tabs=Windows#create-a-pat
        ado_pat = os.environ["AZURE_DEVOPS_BUILD_PAT"]
        organization = os.environ["ADO_ORGANIZATION"]
        project = os.environ["ADO_PROJECT"]
        pipeline_id = os.environ["ADO_AZUREAUTH_LINUX_PIPELINE_ID"]
        stage_id = os.environ["ADO_AZUREAUTH_LINUX_STAGE_ID"]
        version = os.environ["VERSION"]
        debian_revision = os.environ["DEBIAN_REVISION"]
        commit_hash = os.environ["GITHUB_SHA"]
        ado_artifact_name = os.environ["ADO_LINUX_ARTIFACT_NAME"]
        artifact_download_path = os.environ["ADO_LINUX_ARTIFACT_DOWNLOAD_PATH"]

    except KeyError as exc:
        # See https://stackoverflow.com/a/24999035/3288364.
        name = str(exc).replace("'", "")
        sys.exit(f"Error: missing env var: {name}")

    ado_client = ado_connection(organization, ado_pat).clients_v6_0

    # 2. Trigger azure pipeline and wait for it to be finished.
    run_id = trigger_azure_pipeline_and_wait_until_its_completed(
        ado_client,
        organization,
        project,
        pipeline_id,
        stage_id,
        version,
        debian_revision,
        commit_hash,
    )

    # 3. Download the artifact
    download_artifact(
        ado_client, project, run_id, ado_artifact_name, artifact_download_path
    )


if __name__ == "__main__":
    main()
