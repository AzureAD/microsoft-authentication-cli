# Enable a default -Verbose flag for debug output.
[CmdletBinding()]
Param([switch] $NoUpdatePath)

# Halt script execution at the first failed command.
$script:ErrorActionPreference = 'Stop'

# We don't currently have good cross-platform options for determining the latest release version, so we require
# knowledge of the specific target version, which the user should set as an environment variable.
$version = $Env:AZUREAUTH_VERSION
if ([string]::IsNullOrEmpty($version)) {
    Write-Error 'No $AZUREAUTH_VERSION specified, unable to download a release'
}

# Send WM_SETTINGCHANGE after changing Environment variables.
# Refer to https://gist.github.com/alphp/78fffb6d69e5bb863c76bbfc767effda
function Send-SettingChange {
    Add-Type -Namespace Win32 -Name NativeMethods -MemberDefinition @"
    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
    public static extern IntPtr SendMessageTimeout(IntPtr hWnd, uint Msg, UIntPtr wParam, string lParam, uint fuFlags, uint uTimeout, out UIntPtr lpdwResult);
"@
    $HWND_BROADCAST = [IntPtr] 0xffff;
    $WM_SETTINGCHANGE = 0x1a;
    $result = [UIntPtr]::Zero
    
    [void] ([Win32.Nativemethods]::SendMessageTimeout($HWND_BROADCAST, $WM_SETTINGCHANGE, [UIntPtr]::Zero, "Environment", 2, 5000, [ref] $result))
}

function Install-Pre-0-4-0 {
    Write-Verbose "Installing using pre-0.4.0 method"
    $repo = if ([string]::IsNullOrEmpty($Env:AZUREAUTH_REPO)) { 'AzureAD/microsoft-authentication-cli' } else { $Env:AZUREAUTH_REPO }
    $releaseName = "azureauth-${version}-win10-x64"
    $releaseFile = "${releaseName}.zip"
    $releaseUrl = "https://github.com/${repo}/releases/download/${version}/$releaseFile"

    $azureauthDirectory = if ([string]::IsNullOrEmpty($Env:AZUREAUTH_INSTALL_DIRECTORY)) {
        ([System.IO.Path]::Combine($Env:LOCALAPPDATA, "Programs", "AzureAuth"))
    }
    else {
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
    if ($null -ne $ProcessCheck) {
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
        }
        else {
            "${currentPath};${latestDirectory}"
        }
        Set-ItemProperty -Path $registryPath -Name PATH -Value $newPath
        Send-SettingChange
    }
    
    Write-Output "Installed AzureAuth $version!"
}

function Install-Post-0-4-0 {
    Write-Verbose "Installing using post-0.4.0 method"

    $repo = if ([string]::IsNullOrEmpty($Env:AZUREAUTH_REPO)) { 'AzureAD/microsoft-authentication-cli' } else { $Env:AZUREAUTH_REPO }

    # Detect processor architecture (ARM64 or x64)
    # PROCESSOR_ARCHITEW6432 contains the actual architecture when running under emulation
    # PROCESSOR_ARCHITECTURE contains the architecture of the current process
    $arch = if ($env:PROCESSOR_ARCHITECTURE -eq "ARM64") {
        "arm64"
    }
    else {
        "x64"
    }
    Write-Verbose "Detected architecture: $arch (PROCESSOR_ARCHITECTURE=$env:PROCESSOR_ARCHITECTURE, PROCESSOR_ARCHITEW6432=$env:PROCESSOR_ARCHITEW6432)"

    $releaseName = if ([version]::Parse($version) -lt [version]::Parse("0.9.0")) {
        "azureauth-${version}-win10-x64"  # ARM64 not available for versions < 0.9.0
    }
    else {
        "azureauth-${version}-win-${arch}"
    }
    $releaseFile = "${releaseName}.zip"
    $releaseUrl = "https://github.com/${repo}/releases/download/${version}/$releaseFile"
    $azureauthDirectory = if ([string]::IsNullOrEmpty($Env:AZUREAUTH_INSTALL_DIRECTORY)) {
        ([System.IO.Path]::Combine($Env:LOCALAPPDATA, "Programs", "AzureAuth"))
    }
    else {
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

    # To guarantee we start with a fresh install of the requested version we wipe out any pre-existing installation.
    if (Test-Path -Path $targetDirectory) {
        Write-Verbose "Removing pre-existing extracted directory at ${targetDirectory}"
        Remove-Item -Force -Recurse $targetDirectory
    }

    Write-Verbose "Extracting ${zipFile} to ${targetDirectory}"
    Add-Type -AssemblyName System.IO.Compression.FileSystem
    [System.IO.Compression.ZipFile]::ExtractToDirectory($zipFile, $targetDirectory)

    Write-Verbose "Removing ${zipFile}"
    Remove-Item -Force $zipFile

    if ($NoUpdatePath) {
        Write-Verbose "Not updating `$env:PATH"
    }
    else {
        # We only fetch the user's $PATH from the registry and not the system $PATH because we only want to
        # influence the current user's environment. We also fetch the path that `azureauth` is currently installed to
        # in the event that it was previously installed to a custom location.
        $registryPath = 'Registry::HKEY_CURRENT_USER\Environment'
        $currentPath = (Get-ItemProperty -Path $registryPath -Name PATH -ErrorAction SilentlyContinue).Path
        $currentAzureauth = (get-command azureauth -ErrorAction SilentlyContinue).Source
        $currentAzureauthParent = if ($null -ne $currentAzureauth) {
            (get-item $currentAzureauth).Directory.Parent.FullName 
        }        
        $newPath = "";

        # We check to see whether the current $PATH contains either the azureauth installation root or the parent
        # directory of a currently installed `azureauth`.
        if (($null -ne $currentPath) `
                -And ($currentPath.Contains($azureauthDirectory) `
                    -Or (($null -ne $currentAzureauthParent) `
                        -And ($currentPath.Contains($currentAzureauthParent))))) {
            $paths = $currentPath.Split(";")
            $pathArr = @()
            # We reconstruct the $PATH as an array without any azureauth directories.
            ForEach ($path in $paths) {
                if (!(($path.Equals("")) `
                            -Or ($path.Contains($azureauthDirectory)) `
                            -Or (($null -ne $currentAzureauthParent) `
                                -And $currentAzureauthParent.Contains($path)))) {
                    $pathArr += "${path}"
                }
                else {
                    Write-Verbose "Removing '${path}' from `$env:PATH"
                }
            }
            $pathArr += "${targetDirectory}"
            $newPath = $pathArr -join ";"
        }
        else {
            Write-Verbose "Appending '${targetDirectory}' to `$env:PATH"
            if ($null -eq $currentPath) {
                $newPath = "${targetDirectory}"
            }
            else {
                $newPath = "${currentPath};${targetDirectory}"
            }
        }
        Set-ItemProperty -Path $registryPath -Name PATH -Value $newPath
        Send-SettingChange
    }
    Write-Output "Installed AzureAuth $version!"
}

switch ($version) {
    { $_ -in "v0.1.0", "v0.2.0", "v0.3.0", "0.3.1" } {
        Install-Pre-0-4-0
    }
    default {
        Install-Post-0-4-0
    }
}
