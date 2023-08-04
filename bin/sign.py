# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

"""A script which wraps ESRPClient.exe for code signing."""

import json
import os
import subprocess
import sys
from argparse import ArgumentParser
from argparse import ArgumentDefaultsHelpFormatter
from argparse import Namespace
from collections.abc import Iterator
from collections.abc import Generator
from contextlib import ExitStack
from contextlib import contextmanager
from pathlib import Path
from typing import Any
from zipfile import ZipFile
from zipfile import ZIP_DEFLATED

JSON = dict[str, Any]  # A naive type alias for JSON.


def sign_operation(key_code: str, operation: str) -> JSON:
    """Return the JSON signing operation for a given key code/operation."""
    return {
        "KeyCode": key_code,
        "OperationCode": operation,
        "Parameters": {
            "OpusName": "Microsoft",
            "OpusInfo": "http://www.microsoft.com",
            "FileDigest": '/fd "SHA256"',
            "PageHash": "/NPH",
            "TimeStamp": '/tr "http://rfc3161.gtm.corp.microsoft.com/TSS/HttpTspServer" /td sha256',
        },
        "ToolName": "sign",
        "ToolVersion": "1.0",
    }


def sign_operation_linux(key_code: str, operation: str) -> JSON:
    return {
        "KeyCode": key_code,
        "OperationCode": operation,
        "Parameters": {},
        "ToolName": "sign",
        "ToolVersion": "1.0",
    }


def linux_sign(key_code: str) -> JSON:
    """Return the JSON for a `LinuxSign` operation."""
    return sign_operation_linux(key_code, operation="LinuxSign")


def mac_app_developer_sign(key_code: str) -> JSON:
    """Return the JSON for a `MacAppDeveloperSign` operation."""
    return sign_operation(key_code, operation="MacAppDeveloperSign")


def sign_tool_sign(key_code: str) -> JSON:
    """Return the JSON for a `SigntoolSign` operation."""
    return sign_operation(key_code, operation="SigntoolSign")


def sign_tool_verify(key_code: str) -> JSON:
    """Return the JSON for a `SigntoolVerify` operation."""
    return {
        "KeyCode": key_code,
        "OperationCode": "SigntoolVerify",
        "Parameters": {},
        "ToolName": "sign",
        "ToolVersion": "1.0",
    }


def sign_request_file(source: Path, customer_correlation_id: str) -> JSON:
    """Return the JSON for a `SignRequestFiles` entry."""
    return {
        "CustomerCorrelationId": customer_correlation_id,
        "SourceLocation": source.name,
        "DestinationLocation": source.name,
    }


def batch(source: Path, files: list[JSON], operations: list[JSON]) -> JSON:
    """Return a single signing batch for a given set of files and operations."""
    return {
        "SourceLocationType": "UNC",
        "SourceRootDirectory": str(source),
        "DestinationLocationType": "UNC",
        "DestinationRootDirectory": str(source),
        "SignRequestFiles": files,
        "SigningInfo": {"Operations": operations},
    }


@contextmanager
def windows_batches(
    source: Path,
    key_codes: dict[str, str],
    customer_correlation_id: str,
) -> Generator[JSON, None, None]:
    """Yield the JSON signing batches for the win-x64 runtime."""
    files = [
        sign_request_file(path, customer_correlation_id)
        for path in source.iterdir()
        if path.suffix in [".exe", ".dll"] and path.is_file()
    ]

    key_code = key_codes["authenticode"]
    operations = [sign_tool_sign(key_code), sign_tool_verify(key_code)]

    # Yield the batches to ESRPClient.exe signing.
    yield {
        "Version": "1.0.0",
        "SignBatches": [batch(source, files, operations)],
    }


@contextmanager
def osx_batches(
    source: Path,
    key_codes: dict[str, str],
    customer_correlation_id: str,
) -> Generator[JSON, None, None]:
    """Yield the JSON signing batches for the osx-x64 and osx-arm64 runtimes."""
    dlls = []
    dylibs = []
    dylibs_zip = source / "dylibs.zip"

    # Find .dlls and .dylibs (including azureauth).
    for path in source.iterdir():
        if path.suffix == ".dll" and path.is_file():
            dlls.append(path)
        elif (path.name == "azureauth" or path.suffix == ".dylib") and path.is_file():
            dylibs.append(path)

    with ZipFile(dylibs_zip, mode="w", compression=ZIP_DEFLATED) as file:
        for path in dylibs:
            file.write(path, path.relative_to(source))

    dll_files = [sign_request_file(dll, customer_correlation_id) for dll in dlls]
    dylib_files = [sign_request_file(dylibs_zip, customer_correlation_id)]

    authenticode_key_code = key_codes["authenticode"]
    mac_key_code = key_codes["mac"]
    dll_operations = [
        sign_tool_sign(authenticode_key_code),
        sign_tool_verify(authenticode_key_code),
    ]
    dylib_operations = [mac_app_developer_sign(mac_key_code)]

    # Yield the batches to ESRPClient.exe signing.
    yield {
        "Version": "1.0.0",
        "SignBatches": [
            batch(source, dll_files, dll_operations),
            batch(source, dylib_files, dylib_operations),
        ],
    }

    # At this point signing is finished. Extract the signed dylibs.
    with ZipFile(dylibs_zip, mode="r") as file:
        file.extractall(source)
    dylibs_zip.unlink()


@contextmanager
def linux_batches(
    source: Path,
    key_codes: dict[str, str],
    customer_correlation_id: str,
) -> Generator[JSON, None, None]:
    """Yield the JSON signing batches for the linux-x64 runtime."""
    files = [
        sign_request_file(path, customer_correlation_id)
        for path in source.iterdir()
        if path.suffix in [".deb"] and path.is_file()
    ]

    key_code = key_codes["linux"]
    operations = [linux_sign(key_code)]

    # Yield the batches to ESRPClient.exe signing.
    yield {
        "Version": "1.0.0",
        "SignBatches": [batch(source, files, operations)],
    }


def auth(tenant_id: str, client_id: str) -> JSON:
    """Return auth JSON metadata."""
    return {
        "Version": "1.0.0",
        "AuthenticationType": "AAD_CERT",
        "TenantId": tenant_id,
        "ClientId": client_id,
        "AuthCert": {
            "SubjectName": f"CN={client_id}.microsoft.com",
            "StoreLocation": "LocalMachine",
            "StoreName": "My",
            "SendX5c": "true",
        },
        "RequestSigningCert": {
            "SubjectName": f"CN={client_id}",
            "StoreLocation": "LocalMachine",
            "StoreName": "My",
        },
    }


def policy() -> JSON:
    """Return policy JSON metadata."""
    return {
        "Version": "1.0.0",
        "Intent": "Product Release",
        "ContentType": "Signed Binaries",
    }


@contextmanager
def json_tempfile(path: Path, data: JSON) -> Generator[None, None, None]:
    """Create a JSON file with the given data and later remove it."""
    with path.open(mode="w") as file:
        json.dump(obj=data, fp=file, indent=2)
    yield
    path.unlink()


def parse_env_vars(runtime: str) -> tuple[str, str, str, JSON]:
    """Parse and return environment variables"""
    try:
        aad_id = os.environ["SIGNING_AAD_ID"]
        tenant_id = os.environ["SIGNING_TENANT_ID"]
        customer_correlation_id = os.environ["SIGNING_CUSTOMER_CORRELATION_ID"]
        match runtime:
            case "win10-x64":
                # This key code is used for signing .exes and .dlls on both Windows and Mac.
                key_codes = {
                    "authenticode": os.environ["SIGNING_KEY_CODE_AUTHENTICODE"]
                }
            case "osx-x64" | "osx-arm64":
                # SIGNING_KEY_CODE_AUTHENTICODE is used for signing .exes and .dlls on both Windows and Mac.
                # SIGNING_KEY_CODE_MAC is used for signing .dylibs on Macs.
                key_codes = {
                    "authenticode": os.environ["SIGNING_KEY_CODE_AUTHENTICODE"],
                    "mac": os.environ["SIGNING_KEY_CODE_MAC"],
                }
            case "linux-x64" | "linux-arm64":
                # This key code is used for signing .deb on Linux.
                key_codes = {"linux": os.environ["SIGNING_KEY_CODE_LINUX"]}

        return aad_id, tenant_id, customer_correlation_id, key_codes
    except KeyError as exc:
        # See https://stackoverflow.com/a/24999035/3288364.
        name = str(exc).replace("'", "")
        raise KeyError(f"Error: missing env var: {name}")


def parse_args() -> Namespace:
    """Parse and return command line arguments."""
    cwd = Path.cwd()
    parser = ArgumentParser(
        description=__doc__,
        formatter_class=ArgumentDefaultsHelpFormatter,
    )

    parser.add_argument(
        "esrp_client",
        help="the path to the ESRPClient.exe binary",
        type=Path,
    )
    parser.add_argument(
        "--source",
        metavar="SRC",
        help="the source path",
        type=Path,
        default=str(cwd),
    )
    parser.add_argument(
        "--runtime",
        choices=["win10-x64", "osx-x64", "osx-arm64", "linux-x64"],
        help="the runtime of the build in source",
        default="win10-x64",
    )

    return parser.parse_args()


def main() -> None:
    """Determine target runtime, generate inputs, and run ESRPClient.exe."""
    # 1. Parse command line arguments.
    args = parse_args()
    runtime = args.runtime.lower()

    # 2. Read env vars.
    aad_id, tenant_id, customer_correlation_id, key_codes = parse_env_vars(runtime)

    esrp_path = args.esrp_client.resolve()
    source_path = args.source.resolve()
    auth_path = Path("auth.json").resolve()
    policy_path = Path("policy.json").resolve()
    input_path = Path("input.json").resolve()
    output_path = Path("output.json").resolve()

    # 3. Determine runtime & create a batchmaker.
    match runtime:
        case "win10-x64":
            batchmaker = windows_batches(
                source=source_path,
                key_codes=key_codes,
                customer_correlation_id=customer_correlation_id,
            )
        case "osx-x64" | "osx-arm64":
            batchmaker = osx_batches(
                source=source_path,
                key_codes=key_codes,
                customer_correlation_id=customer_correlation_id,
            )
        case "linux-x64":
            batchmaker = linux_batches(
                source=source_path,
                key_codes=key_codes,
                customer_correlation_id=customer_correlation_id,
            )
        case _:
            # This should be unreachable because of argparse, but let's be safe.
            sys.exit(f"Error: Invalid runtime: {args.runtime}")

    # 4. Create the necessary context and run ESRPClient.
    esrp_args = [
        str(esrp_path),
        "sign",
        "-a",
        str(auth_path),
        "-i",
        str(input_path),
        "-p",
        str(policy_path),
        "-o",
        str(output_path),
        "-l",
        "Progress",
    ]

    # All temporary files created in this context should be cleaned up.
    with ExitStack() as stack:
        # Generate auth.json.
        auth_json = auth(tenant_id, aad_id)
        stack.enter_context(json_tempfile(auth_path, auth_json))

        # Generate policy.json.
        policy_json = policy()
        stack.enter_context(json_tempfile(policy_path, policy_json))

        # Generate input.json (and any supporting intermediate files).
        batches = stack.enter_context(batchmaker)
        stack.enter_context(json_tempfile(input_path, batches))

        # Run ESRPClient.exe.
        subprocess.run(esrp_args, check=True)


if __name__ == "__main__":
    main()
