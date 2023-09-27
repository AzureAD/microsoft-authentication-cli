# Enable a default -Verbose flag for debug output.
[CmdletBinding()]
Param([switch] $NoUpdatePath)

# Halt script execution at the first failed command.
$script:ErrorActionPreference='Stop'

function Uninstall {
    $uninstallDirectory = Get-UninstallDirectory
    Remove-InstallationFolder $uninstallDirectory
    Remove-FromPath $uninstallDirectory
    Write-Output "Uninstalled AzureAuth!"
}

function Get-UninstallDirectory {
    $directory = if (![string]::IsNullOrEmpty($Env:AZUREAUTH_INSTALL_DIRECTORY)) {
        Write-Error "Uninstalling from custom location is not supported"
    } 
    return ([System.IO.Path]::Combine($Env:LOCALAPPDATA, "Programs", "AzureAuth"))
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
    param ([string]$uninstallDirectory)

    $registryPath = 'Registry::HKEY_CURRENT_USER\Environment'
    $currentPath = (Get-ItemProperty -Path $registryPath -Name PATH -ErrorAction SilentlyContinue).Path

    # We try to find any other AzureAuth installations that are not in default location.
    # Installations in custom locations that are not listed in PATH cannot be found.
    $azureauthsInPath = (Get-Command -Name azureauth -ErrorAction SilentlyContinue -CommandType Application -All).Source
    $otherDirectories = [System.Collections.ArrayList]@()
    ForEach($az in $azureauthsInPath) {
        if (!$az.Contains($uninstallDirectory)) {
            $additionalDirectory = (Get-Item $az).Directory.FullName
            Write-Verbose "Additional installation found in ${additionalDirectory}"
            $index = $otherDirectories.Add($additionalDirectory)
            Remove-InstallationFolder ($additionalDirectory)
        }
    }

    # Reconstruct the $PATH without any azureauth directories.
    $updatedPath = "";
    if (($null) -ne $currentPath) {
        $paths = $currentPath.Split(";")
        $pathArr = @()
        ForEach($path in $paths){
            if(!$path.Equals("") -And !$path.Contains($uninstallDirectory) -And ` 
                    !$otherDirectories.Contains($path)) {
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
    Write-Verbose "Custom installations that were not in `$env:Path could not be deleted."
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
