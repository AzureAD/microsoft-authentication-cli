# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

import json
import os
import glob
import pprint
import subprocess
import sys
from pathlib import Path
import zipfile

AAD_ID = os.environ['AZURE_AAD_ID']
WORKSPACE = Path(os.environ["WORKSPACE"])
TENANT_ID = os.environ['TENANT_ID']
KEY_CODE = os.environ['KEY_CODE']

esrp_tool = os.environ['ESRP_CLIENT']
SOURCE =  WORKSPACE / "osx-x64"
DESTINATION = WORKSPACE / "Mac_signed"

zip_file = SOURCE / "mac_dylibs.zip"
extensions = [".dylib"]

 # zipping the files
with zipfile.ZipFile(zip_file, 'w', zipfile.ZIP_DEFLATED) as zip_obj:
    for path in Path(SOURCE).iterdir():
        if (path.name == "azureauth" or path.suffix in extensions) and path.is_file():
            zip_obj.write(path, path.relative_to(SOURCE))

if not zip_file.exists(): 
	sys.exit("Error: cannot find file to sign")
else:
    print(f"Found file: {zip_file}")


auth_json = {
	"Version": "1.0.0",
	"AuthenticationType": "AAD_CERT",
	"TenantId": TENANT_ID,
	"ClientId": AAD_ID, 
	"AuthCert": {
		"SubjectName": f"CN={AAD_ID}.microsoft.com", 
		"StoreLocation": "CurrentUser", 
		"StoreName": "My",
	},
	"RequestSigningCert": {
		"SubjectName": f"CN={AAD_ID}", 
		"StoreLocation": "CurrentUser",
		"StoreName": "My",
	}
}

input_json = {
	"Version": "1.0.0",
	"SignBatches": [
		{
			"SourceLocationType": "UNC",
			"SourceRootDirectory": str(SOURCE),
			"DestinationLocationType": "UNC",
			"DestinationRootDirectory": str(DESTINATION),
			"SignRequestFiles": [
				{
					"CustomerCorrelationId": "01A7F55F-6CDD-4123-B255-77E6F212CDAD", 
					"SourceLocation": str(zip_file), 
					"DestinationLocation": str(DESTINATION / "mac_dylibs.zip"),
				}
			],
			"SigningInfo": {
				"Operations": [
					{
						"KeyCode": KEY_CODE, 
						"OperationCode": "MacAppDeveloperSign", 
						"Parameters" : {
							"OpusName" : "Microsoft",
							"OpusInfo" : "http://www.microsoft.com",
							"FileDigest" : "/fd \"SHA256\"",
							"PageHash" : "/NPH",
							"TimeStamp" : "/tr \"http://rfc3161.gtm.corp.microsoft.com/TSS/HttpTspServer\" /td sha256"
           				 },
						"ToolName": "sign",
						"ToolVersion": "1.0", 
					}
				]

			}
		}
	]
}

policy_json = { 
	"Version": "1.0.0",
	"Intent": "production release", 
	"ContentType": "Signed Binaries",
}

configs = [
	("auth.json", auth_json),
	("input.json", input_json),
	("policy.json", policy_json),
]

for filename, data in configs:
	with open(filename, 'w') as fp:
		json.dump(data, fp)

# Run ESRP Client
esrp_out = "esrp_out.json"
result = subprocess.run(
	[esrp_tool, "sign",
	"-a", "auth.json",
	"-i", "input.json",
	"-p", "policy.json",
	"-o", esrp_out,
	"-l", "Verbose"],
	cwd=WORKSPACE) 

if result.returncode != 0:
	sys.exit("Failed to run ESRPClient.exe")

if os.path.isfile(esrp_out):
	print("ESRP output json:")
	with open(esrp_out, 'r') as fp:
		pprint.pp(json.load(fp))

signed_zip_file = os.path.join(DESTINATION, "mac_dylibs.zip")

if not signed_zip_file: 
	sys.exit("Error: no signed file found")
else:
    print(f"The Zipped file with signed binaries: {signed_zip_file}")

#Extracting all the signed file and removing the zip file to cleanup temporary files
with zipfile.ZipFile(signed_zip_file, 'r') as zipObj:
   zipObj.extractall(DESTINATION)

Path(signed_zip_file).unlink()

#list of signed files
signed_binaries = [f for f in DESTINATION.iterdir() if os.path.isfile(f)]

if not signed_binaries: 
	sys.exit("Error: no signed files found")
   
print(f"Signed {len(signed_binaries)} files:")
pprint.pp(signed_binaries)
