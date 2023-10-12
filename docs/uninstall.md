# Uninstalling AzureAuth

We provide an uninstall script you can use to Uninstall AzureAuth. The script removes the default AzureAuth reference in the PATH and it will deletes the AzureAuth installation folder.

The script currently doesn't support uninstalling from custom locations. This is to avoid removing any potential files that are not safe to delete. If there are any potential installations in custom locations found in the PATH (installed through the `$AZUREAUTH_INSTALL_DIRECTORY` environment variable), a warning will be printed showing its location. Installations in custom locations that are not listed in the PATH cannot be found and uninstalled.

## Usage

Run the following in PowerShell:

```PowerShell
$script = "${env:TEMP}\uninstall.ps1"
$url = "https://raw.githubusercontent.com/AzureAD/microsoft-authentication-cli/main/install/uninstall.ps1"
[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
Invoke-WebRequest $url -OutFile $script; if ($?) { &$script }; if ($?) { rm $script }
```

## Note

* The script doesn't work for versions previous to AzureAuth 0.4.0
* The script won't work as intended if you try to uninstall right after installing because PATH changes from the installation won't take effect until after restarting PowerShell.