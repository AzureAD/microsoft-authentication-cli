# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

from datetime import datetime
from os import environ

now = datetime.now()

# This local version is used when building and packaging locally on a developer machine.
# In actual PR and CI pipelines we use the BUILD_BUILDNUMBER env var defined by the pipeline
LOCAL_VERSION = f"{now.year}.{now.month}.{now.day}.{now.hour}-{now.minute}{now.second}-local"

BUILD_VERSION_NUMBER_VAR = "BUILD_BUILDNUMBER"

def get_version():
    if BUILD_VERSION_NUMBER_VAR in environ:
        return environ[BUILD_VERSION_NUMBER_VAR]
    return LOCAL_VERSION

def print_header(message: str) -> None:
    header = "-" * len(message)
    print(f"{message}\n{header}", flush=True)
