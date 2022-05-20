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


def mac_app_developer_sign(key_code: str) -> JSON:
    """Return the JSON for a `MacAppDeveloperSign` operation."""
    return {
        "KeyCode": key_code,
        "OperationCode": "MacAppDeveloperSign",
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


def sign_tool_sign(key_code: str) -> JSON:
    """Return the JSON for a `SigntoolSign` operation."""
    return {
        "KeyCode": key_code,
        "OperationCode": "SigntoolSign",
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


def sign_tool_verify(key_code: str) -> JSON:
    """Return the JSON for a `SigntoolVerify` operation."""
    return {
        "KeyCode": key_code,
        "OperationCode": "SigntoolVerify",
        "Parameters": {},
        "ToolName": "sign",
        "ToolVersion": "1.0",
    }


def sign_request_file(
    source: Path,
    destination: Path,
    customer_correlation_id: str,
) -> JSON:
    """Return the JSON for a `SignRequestFiles` entry."""
    return {
        "CustomerCorrelationId": customer_correlation_id,
        "SourceLocation": source.name,
        "DestinationLocation": destination.name,
    }


def batch(
    source: Path,
    destination: Path,
    files: list[JSON],
    operations: list[JSON],
) -> JSON:
    """Return a single signing batch for a given set of files and operations."""
    return {
        "SourceLocationType": "UNC",
        "SourceRootDirectory": str(source),
        "DestinationLocationType": "UNC",
        "DestinationRootDirectory": str(destination),
        "SignRequestFiles": files,
        "SigningInfo": {"Operations": operations},
    }


@contextmanager
def windows_batches(
    source: Path,
    destination: Path,
    key_code: str,
    customer_correlation_id: str,
) -> Generator[JSON, None, None]:
    """Yield the JSON signing batches for the win-x64 runtime."""
    files = [
        sign_request_file(path, path, customer_correlation_id)
        for path in source.iterdir()
        if path.suffix in [".exe", ".dll"] and path.is_file()
    ]

    operations = [sign_tool_sign(key_code), sign_tool_verify(key_code)]

    # Yield the batches to ESRPClient.exe signing.
    yield {
        "Version": "1.0.0",
        "SignBatches": [batch(source, destination, files, operations)],
    }


@contextmanager
def osx_batches(
    source: Path,
    destination: Path,
    key_code: str,
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

    dll_files = [sign_request_file(dll, dll, customer_correlation_id) for dll in dlls]
    dylib_files = [sign_request_file(dylibs_zip, dylibs_zip, customer_correlation_id)]

    dll_operations = [sign_tool_sign(key_code), sign_tool_verify(key_code)]
    dylib_operations = [mac_app_developer_sign(key_code)]

    # Yield the batches to ESRPClient.exe signing.
    yield {
        "Version": "1.0.0",
        "SignBatches": [
            batch(source, destination, dll_files, dll_operations),
            batch(source, destination, dylib_files, dylib_operations),
        ],
    }

    # At this point signing is finished. Extract the signed dylibs.
    dylibs_zip = destination / dylibs_zip.name
    with ZipFile(dylibs_zip, mode="r") as file:
        file.extractall(destination)
    dylibs_zip.unlink()


def auth(tenant_id: str, client_id: str) -> JSON:
    """Return auth JSON metadata."""
    return {
        "Version": "1.0.0",
        "AuthenticationType": "AAD_CERT",
        "TenantId": tenant_id,
        "ClientId": client_id,
        "AuthCert": {
            "SubjectName": f"CN={client_id}.microsoft.com",
            "StoreLocation": "CurrentUser",
            "StoreName": "My",
        },
        "RequestSigningCert": {
            "SubjectName": f"CN={client_id}",
            "StoreLocation": "CurrentUser",
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


def parse_args() -> Namespace:
    """Parse and return command line arguments."""
    cwd = Path.cwd()
    parser = ArgumentParser(
        description=__doc__, formatter_class=ArgumentDefaultsHelpFormatter
    )

    parser.add_argument("esrp_client", help="the path to the ESRPClient.exe binary")
    parser.add_argument(
        "--source",
        metavar="SRC",
        help="the source path",
        type=Path,
        default=str(cwd),
    )
    parser.add_argument(
        "--destination",
        metavar="DST",
        help="the destination path",
        type=Path,
        default=str(cwd),
    )
    parser.add_argument(
        "--runtime",
        choices=["win-x64", "osx-x64", "osx-arm64"],
        help="the runtime of the build in source",
        default="win-x64",
    )

    return parser.parse_args()


def main() -> None:
    """Determine target runtime, generate inputs, and run ESRPClient.exe."""
    # 1. Parse command line arguments.
    args = parse_args()

    # 2. Read env vars.
    try:
        aad_id = os.environ["SIGNING_AAD_ID"]
        tenant_id = os.environ["SIGNING_TENANT_ID"]
        key_code = os.environ["SIGNING_KEY_CODE"]
        customer_correlation_id = os.environ["SIGNING_CUSTOMER_CORRELATION_ID"]
    except KeyError as exc:
        # See https://stackoverflow.com/a/24999035/3288364.
        name = str(exc).replace("'", "")
        sys.exit(f"Error: missing env var: {name}")

    source_path = args.source.resolve()
    destination_path = args.destination.resolve()
    auth_path = Path("auth.json")
    policy_path = Path("policy.json")
    input_path = Path("input.json")
    output_path = Path("output.json")

    # 3. Determine runtime & create a batchmaker.
    match args.runtime.lower():
        case "win-x64":
            batchmaker = windows_batches(
                source=source_path,
                destination=destination_path,
                key_code=key_code,
                customer_correlation_id=customer_correlation_id,
            )
        case "osx-x64" | "osx-arm64":
            batchmaker = osx_batches(
                source=source_path,
                destination=destination_path,
                key_code=key_code,
                customer_correlation_id=customer_correlation_id,
            )
        case _:
            # This should be unreachable because of argparse, but let's be safe.
            sys.exit(f"Error: Invalid runtime: {args.runtime}")

    # 4. Create the necessary context and run ESRPClient.
    esrp_args = [
        args.esrp_client,
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
        "Verbose",
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
        subprocess.run(esrp_args)


if __name__ == "__main__":
    main()
