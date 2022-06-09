# Enable a default -Verbose flag for debug output.
[CmdletBinding()]
Param()

# Halt script execution at the first failed command.
$script:ErrorActionPreference='Stop'

# We don't currently have good cross-platform options for determining the latest release version, so we require
# knowledge of the specific target version, which the user should set as an environment variable.
$version = $Env:AZUREAUTH_VERSION
if ([string]::IsNullOrEmpty($version)) {
    Write-Error 'No $AZUREAUTH_VERSION specified, unable to download a release'
}

$repo = if ([string]::IsNullOrEmpty($Env:AZUREAUTH_REPO)) { 'AzureAD/microsoft-authentication-cli' } else { $Env:AZUREAUTH_REPO }
$releaseName = "azureauth-${version}-win10-x64"
$releaseFile = "${releaseName}.zip"
$releaseUrl = "https://github.com/${repo}/releases/download/${version}/$releaseFile"

$azureauthDirectory = if ([string]::IsNullOrEmpty($Env:AZUREAUTH_INSTALL_DIRECTORY)) {
    ([System.IO.Path]::Combine($Env:LOCALAPPDATA, "Programs", "AzureAuth"))
} else {
    $Env:AZUREAUTH_INSTALL_DIRECTORY
}
$extractedDirectory = ([System.IO.Path]::Combine($azureauthDirectory, $releaseName))
$latestDirectory = ([System.IO.Path]::Combine($azureauthDirectory, "latest"))
$zipFile = ([System.IO.Path]::Combine($azureauthDirectory, $releaseFile))

Write-Verbose "Creating ${azureauthDirectory}"
$null = New-Item -ItemType Directory -Force -Path $azureauthDirectory

Write-Verbose "Downloading ${releaseUrl} to ${zipFile}"
$client = New-Object System.Net.WebClient
$client.DownloadFile($releaseUrl, $zipFile)

# A running instance of azureauth can cause installation to fail, so we try to kill any running instances first.
# We suppress taskkill output here because this is a best effort attempt and we don't want the user to see its output.
# Here, Get-Process is used to first determine whether there is an existing azureauth process. If there is, kill the existing process first.
$ProcessCheck = Get-Process -Name azureauth -ErrorAction SilentlyContinue -ErrorVariable ProcessError
if ($ProcessCheck -ne $null)
{
    Write-Verbose "Stopping any currently running azureauth instances"
    taskkill /f /im azureauth.exe 2>&1 | Out-Null
}

if (Test-Path -Path $extractedDirectory) {
    Write-Verbose "Removing pre-existing extracted directory at ${extractedDirectory}"
    Remove-Item -Force -Recurse $extractedDirectory
}

Write-Verbose "Extracting ${zipFile} to ${extractedDirectory}"
Add-Type -AssemblyName System.IO.Compression.FileSystem
[System.IO.Compression.ZipFile]::ExtractToDirectory($zipFile, $azureauthDirectory)

if (Test-Path -Path $latestDirectory) {
    Write-Verbose "Removing pre-existing latest directory at ${latestDirectory}"
    
    # We use IO.Directory::Delete instead of Remove-Item because on Windows Server 2012 with PowerShell 4.0 the latter will not work.
    [IO.Directory]::Delete($latestDirectory)
}

# We use a directory junction here because not all Windows users will have permissions to create a symlink.
# We create this junction with cmd.exe's mklink because it has a stable interface across all active versions of Windows and Windows Server,
# while PowerShell's New-Item has breaking changes and doesn't have the -Target param in 4.0 (the default PowerShell on Win Server 2012).

Write-Verbose "Linking ${latestDirectory} to ${extractedDirectory}"
cmd.exe /Q /C "mklink /J `"$latestDirectory`" `"$extractedDirectory`"" > $null
if (!$?) {
    Write-Error "Linking failed!"
}

Write-Verbose "Removing ${zipFile}"
Remove-Item -Force $zipFile

# Permanently add the latest directory to the current user's $PATH (if it's not already there).
# Note that this will only take effect when a new terminal is started.
$registryPath = 'Registry::HKEY_CURRENT_USER\Environment'
$currentPath = (Get-ItemProperty -Path $registryPath -Name PATH -ErrorAction SilentlyContinue).Path
if ($currentPath -NotMatch 'AzureAuth') {
    Write-Verbose "Updating `$PATH to include ${latestDirectory}"
    $newPath = if ($currentPath -Match $null) {
        "${latestDirectory}"
    } else {
        "${currentPath};${latestDirectory}"
    }
    Set-ItemProperty -Path $registryPath -Name PATH -Value $newPath
}

Write-Output "Installed azureauth $version!"
