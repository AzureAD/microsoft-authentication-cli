# Enable a default -Verbose flag for debug output.
[CmdletBinding()]

# Halt script execution at the first failed command.
$script:ErrorActionPreference='Stop'

$azureauthDefaultLocation = ([System.IO.Path]::Combine($Env:LOCALAPPDATA, "Programs", "AzureAuth"))

function Uninstall {
    Close-AzureauthInstances

    $locations = Get-AzureauthsInPath
    Remove-AzureauthDirectories $locations
    Remove-FromPath $locations
    
    Write-Output "Uninstalled AzureAuth!"
}

function Get-AzureauthsInPath {
    $allLocations = [System.Collections.ArrayList]@()
    $azureauthsInPath = (Get-Command -Name azureauth -ErrorAction SilentlyContinue -CommandType Application -All).Source

    # Uninstallation is only supported from the default AzureAuth location.
    # We warn the user of any custom locations that are listed in the PATH.
    # Installations in custom locations that are not listed in PATH cannot
    # be found and uninstalled.
    ForEach($az in $azureauthsInPath) {
        if (!$az.Contains($azureauthDefaultLocation)) {
            $additionalDirectory = (Get-Item $az).Directory.FullName
            Write-Warning "Additional installation found in ${additionalDirectory}"
        } else {
            # We add the PATH location to the list and not the default location to later
            # remove it from the PATH (default location is the whole parent folder).
            $_ = $allLocations.Add($az)
        }
    }

    return $allLocations
}

function Remove-AzureauthDirectories {
    param([System.Collections.ArrayList]$locations)

    ForEach ($location in $locations) {
        Remove-InstallationFolder $location
    }
    
    # We also delte AzureAuth parent folder
    Remove-InstallationFolder $azureauthDefaultLocation
}

function Remove-InstallationFolder {
    param ([string]$directory)

    if (Test-Path -Path $directory) {
        Write-Verbose "Removing installations at '${directory}'"
        Remove-Item -Force -Recurse $directory
    } else {
        Write-Verbose "There were no installations found at '${directory}'"
    }
}

function Remove-FromPath {
    param ([System.Collections.ArrayList]$locationsToDelete)

    $registryPath = 'Registry::HKEY_CURRENT_USER\Environment'
    $currentPath = (Get-ItemProperty -Path $registryPath -Name PATH -ErrorAction SilentlyContinue).Path

    # Reconstruct the $PATH without any azureauth directories.
    $updatedPath = "";
    if (($null) -ne $currentPath) {
        $paths = $currentPath.Split(";")
        $pathArr = @()
        ForEach($path in $paths){
            if(!$path.Equals("") -And ($locationsToDelete.Count -eq 0 -Or !$locationsToDelete.Contains($path))) {
                $pathArr += "${path}"
            }
            elseif (!$path.Equals("")) {
                Write-Verbose "Removing '${path}' from `$env:PATH"
            }
        }
        $updatedPath = ($pathArr -join ";") + ";"
    }

    Set-ItemProperty -Path $registryPath -Name PATH -Value $updatedPath
    Send-SettingChange
}

function Close-AzureauthInstances {
    # Uninstall will fail if there are instances of AzureAuth running. 
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

Uninstall
