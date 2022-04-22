# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

import json
import os
import glob
import pprint
import subprocess
import sys
from pathlib import Path

AAD_ID = os.environ['AZURE_AAD_ID']
WORKSPACE = Path(os.environ['WORKSPACE'])
TENANT_ID = os.environ['TENANT_ID']
KEY_CODE = os.environ['KEY_CODE']

esrp_tool = os.environ['ESRP_CLIENT']
SOURCE = WORKSPACE / "win10-x64"
DESTINATION = WORKSPACE

files = []
extensions = [".exe", ".dll"]
for path in Path(SOURCE).iterdir():
    if path.suffix in extensions and path.is_file():
        files.append(path)

if not files: #empty list check
	sys.exit("Error: cannot find files to sign")
	
print(f"Found {len(files)} files:")
pprint.pp(files)

files_to_sign = [os.path.basename(f) for f in files] 

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
					"SourceLocation": f, 
					"DestinationLocation": os.path.join("Signed", f),
				}
				for f in files_to_sign
			],
			"SigningInfo": {
				"Operations": [
					{
						"KeyCode": KEY_CODE, 
						"OperationCode": "SigntoolSign", 
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

signed_files_location = DESTINATION / "Signed"

signed_files = glob.glob(signed_files_location + '**/*')
signed_files = [f for f in signed_files if os.path.isfile(f)]

if not signed_files: 
	sys.exit("Error: no signed files found")
   
print(f"Signed {len(signed_files)} files:")
pprint.pp(signed_files)
