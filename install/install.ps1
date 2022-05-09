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

# Without this, System.Net.WebClient.DownloadFile will fail on a client with TLS 1.0/1.1 disabled
if ([Net.ServicePointManager]::SecurityProtocol.ToString().Split(',').Trim() -notcontains 'Tls12') {
    [Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12 | [Net.ServicePointManager]::SecurityProtocol
}

Write-Verbose "Downloading ${releaseUrl} to ${zipFile}"
$client = New-Object System.Net.WebClient
$client.DownloadFile($releaseUrl, $zipFile)

if (Test-Path -Path $extractedDirectory) {
    Write-Verbose "Removing pre-existing extracted directory at ${extractedDirectory}"
    Remove-Item -Force -Recurse $extractedDirectory
}

Write-Verbose "Extracting ${zipFile} to ${extractedDirectory}"
Add-Type -AssemblyName System.IO.Compression.FileSystem
[System.IO.Compression.ZipFile]::ExtractToDirectory($zipFile, $azureauthDirectory)

if (Test-Path -Path $latestDirectory) {
    Write-Verbose "Removing pre-existing latest directory at ${latestDirectory}"
    Remove-Item -Force -Recurse $latestDirectory
}

# We use a directory junction here because not all Windows users will have permissions to create a symlink.
Write-Verbose "Linking ${latestDirectory} to ${extractedDirectory}"
$null = New-Item -Path $latestDirectory -Target $extractedDirectory -ItemType Junction

Write-Verbose "Removing ${zipFile}"
Remove-Item -Force $zipFile

# Permanently add the latest directory to the current user's $PATH (if it's not already there).
# Note that this will only take effect when a new terminal is started.
$registryPath = 'Registry::HKEY_CURRENT_USER\Environment'
$currentPath = (Get-ItemProperty -Path $registryPath -Name PATH).Path
if ($currentPath -NotMatch 'AzureAuth') {
    Write-Verbose "Updating `$PATH to include ${latestDirectory}"
    $newPath = "${currentPath};${latestDirectory}"
    Set-ItemProperty -Path $registryPath -Name PATH -Value $newPath
}

Write-Output "Installed azureauth $version!"
