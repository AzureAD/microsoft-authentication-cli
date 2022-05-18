"""A script which wraps ESRPClient.exe for code signing."""

import argparse
import json
import os
import subprocess
import sys
from collections.abc import Iterator
from pathlib import Path
from typing import Any

JSON = dict[str, Any]  # A  naive type alias for JSON.


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


def batch(
    source: Path, destination: Path, files: list[JSON], operations: list[JSON]
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


def windows_batches(
    source: Path, destination: Path, key_code: str, customer_correlation_id: str
) -> list[JSON]:
    """Return the JSON signing batches for the Windows platform."""
    extensions = [".exe", ".dll"]
    files = [
        {
            "CustomerCorrelationId": customer_correlation_id,
            "SourceLocation": path.name,
            "DestinationLocation": path.name,
        }
        for path in source.iterdir()
        if path.suffix in extensions and path.is_file()
    ]

    operations = [
        sign_tool_sign(key_code),
        sign_tool_verify(key_code),
    ]

    return [batch(source, destination, files, operations)]


def esrp_input(batches: list[JSON]) -> JSON:
    """Return the top-level JSON for an ESRPClient.exe input file."""
    return {"Version": "1.0.0", "SignBatches": batches}


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


def parse_args() -> argparse.Namespace:
    """Parse and return command line arguments."""
    cwd = Path.cwd()
    parser = argparse.ArgumentParser()

    parser.add_argument("esrp_client")
    parser.add_argument("--source", default=str(cwd))
    parser.add_argument("--destination", default=str(cwd))
    parser.add_argument("--platform", choices=["windows", "macos"], default="windows")

    return parser.parse_args()


def main() -> None:
    """Determine target platform, generate inputs, and run ESRPClient.exe."""
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

    source_path = Path(args.source).resolve()
    destination_path = Path(args.destination).resolve()
    auth_path = Path("auth.json")
    policy_path = Path("policy.json")
    input_path = Path("input.json")
    output_path = Path("output.json")

    # 3. Determine platform & generate signing batches.
    batches = []

    match args.platform.lower():
        case "windows":
            batches = windows_batches(
                source=source_path,
                destination=destination_path,
                key_code=key_code,
                customer_correlation_id=customer_correlation_id,
            )
        case "macos":
            sys.exit("Error: Platform 'macos' not yet implemented!")
        case _:
            # This should be unreachable because of argparse, but let's be safe.
            sys.exit(f"Error: Invalid platform: {args.platform}")

    # 4. Create input.json
    with input_path.open(mode="w") as file:
        obj = esrp_input(batches=batches)
        json.dump(obj=obj, fp=file, indent=2)

    # 5. Create auth.json
    with auth_path.open(mode="w") as file:
        obj = auth(tenant_id=tenant_id, client_id=aad_id)
        json.dump(obj=obj, fp=file, indent=2)

    # 6. Create policy.json.
    with policy_path.open(mode="w") as file:
        obj = policy()
        json.dump(obj=obj, fp=file, indent=2)

    # 7. Run ESRPClient.
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

    subprocess.run(esrp_args)

    # 8. TODO: Maybe clean up any temporary files?


if __name__ == "__main__":
    main()
