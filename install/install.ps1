# Enable a default -Verbose flag for debug output.
[CmdletBinding()]
Param([switch] $NoUpdatePath)

# Halt script execution at the first failed command.
$script:ErrorActionPreference='Stop'

# We don't currently have good cross-platform options for determining the latest release version, so we require
# knowledge of the specific target version, which the user should set as an environment variable.

$version = $Env:AZUREAUTH_VERSION
if ([string]::IsNullOrEmpty($version)) {
    Write-Error 'No $AZUREAUTH_VERSION specified, unable to download a release'
}

function Install-Pre-0-4-0 {

    Write-Verbose "Installing using pre-0.4.0 method"
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
    if ($null -ne $ProcessCheck)
    {
        Write-Verbose "Stopping any currently running azureauth instances"
        taskkill /f /im azureauth.exe 2>&1 | Out-Null

        # After killing the process it is still possible for there there to be locks on the files it was using (including
        # its own DLLs). The OS may take an indeterminate amount of time to clean those up, but so far we've observed 1
        # second to be enough.
        Start-Sleep -Seconds 1
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
        Write-Verbose "Updating `$env:PATH to include ${latestDirectory}"
        $newPath = if ($null -eq $currentPath) {
            "${latestDirectory}"
        } else {
            "${currentPath};${latestDirectory}"
        }
        setx PATH $newPath > $null
    }

    Write-Output "Installed azureauth $version!"

}

function Install-Post-0-4-0 {

    Write-Verbose "Installing using post-0.4.0 method"

    $repo = if ([string]::IsNullOrEmpty($Env:AZUREAUTH_REPO)) { 'AzureAD/microsoft-authentication-cli' } else { $Env:AZUREAUTH_REPO }
    $releaseName = "azureauth-${version}-win10-x64"
    $releaseFile = "${releaseName}.zip"
    $releaseUrl = "https://github.com/${repo}/releases/download/${version}/$releaseFile"

    $azureauthDirectory = if ([string]::IsNullOrEmpty($Env:AZUREAUTH_INSTALL_DIRECTORY)) {
        ([System.IO.Path]::Combine($Env:LOCALAPPDATA, "Programs", "AzureAuth"))
    } else {
        $Env:AZUREAUTH_INSTALL_DIRECTORY
    }
    
    $targetDirectory = ([System.IO.Path]::Combine($azureauthDirectory, $version))
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
    if ($null -ne $ProcessCheck) {
        Write-Verbose "Stopping any currently running azureauth instances"
        taskkill /f /im azureauth.exe 2>&1 | Out-Null

        # After killing the process it is still possible for there there to be locks on the files it was using (including
        # its own DLLs). The OS may take an indeterminate amount of time to clean those up, but so far we've observed 1
        # second to be enough.
        Start-Sleep -Seconds 1
    }

    if (Test-Path -Path $targetDirectory) {
        Write-Verbose "Removing pre-existing extracted directory at ${targetDirectory}"
        Remove-Item -Force -Recurse $targetDirectory
    }

    Write-Verbose "Extracting ${zipFile} to ${targetDirectory}"
    Add-Type -AssemblyName System.IO.Compression.FileSystem
    [System.IO.Compression.ZipFile]::ExtractToDirectory($zipFile, $targetDirectory)

    Write-Verbose "Removing ${zipFile}"
    Remove-Item -Force $zipFile

    if (!$NoUpdatePath) {
        $registryPath = 'Registry::HKEY_CURRENT_USER\Environment'
        $currentPath = (Get-ItemProperty -Path $registryPath -Name PATH -ErrorAction SilentlyContinue).Path
        $currentAzureauth = (get-command azureauth -ErrorAction SilentlyContinue).Source
        $currentAzureauthParent = if($null -ne $currentAzureauth) {
            (get-item $currentAzureauth).Directory.Parent.FullName }
        
        $newPath = "";

        if (($null -ne $currentPath) -And ($currentPath.Contains($azureauthDirectory) -Or $currentPath.Contains($currentAzureauthParent))) {
            $paths = $currentPath.Split(";")
            $pathArr = @()
            ForEach($path in $paths){
                if(!(($path.Equals("")) -Or ($path.Contains($azureauthDirectory)) -Or (($null -ne $currentAzureauthParent) -And $currentAzureauthParent.Contains($path)))){
                    $pathArr += "${path}"
                }
                else {
                    Write-Verbose "Removing ${path} from `$env:PATH"
                }
            }
            $pathArr += "${targetDirectory}"
            $newPath = $pathArr -join ";"
        }
        else {
            Write-Verbose "Appending ${targetDirectory} to `$env:PATH"
            if ($null -eq $currentPath) {
                $newPath = "${targetDirectory}"
            } else {
                $newPath = "${currentPath};${targetDirectory}"
            }
        }

        setx PATH $newPath > $null
    }

    Write-Output "Installed azureauth $version!"

}

switch ($version) {
     { $_ -in "v0.1.0","v0.2.0","v0.3.0","0.3.1" } {
        Install-Pre-0-4-0
    }
    default {
        Install-Post-0-4-0
    }
}