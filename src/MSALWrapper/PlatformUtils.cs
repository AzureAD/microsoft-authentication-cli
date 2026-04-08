// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Authentication.MSALWrapper
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Runtime.InteropServices;
    using Microsoft.Extensions.Logging;

    /// <summary>
    /// A class for checking platform information.
    /// </summary>
    public class PlatformUtils : IPlatformUtils
    {
        private ILogger logger;
        private Lazy<bool> isWindows;
        private Lazy<bool> isWindows10;
        private Lazy<bool> isMacOS;
        private Lazy<bool> isMacOSBrokerAvailable;

        /// <summary>
        /// Initializes a new instance of the <see cref="PlatformUtils"/> class.
        /// </summary>
        /// <param name="logger">An <see cref="ILogger"/> to use.</param>
        public PlatformUtils(ILogger logger)
        {
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.isWindows = new Lazy<bool>(() => this.CheckWindows());
            this.isWindows10 = new Lazy<bool>(() => this.CheckWindows10());
            this.isMacOS = new Lazy<bool>(() => this.CheckMacOS());
            this.isMacOSBrokerAvailable = new Lazy<bool>(() => this.CheckMacOSBrokerAvailable());
        }

        /// <inheritdoc/>
        public bool IsWindows10Or11()
        {
            return this.isWindows10.Value;
        }

        /// <inheritdoc/>
        public bool IsWindows()
        {
            return this.isWindows.Value;
        }

        /// <inheritdoc/>
        public bool IsMacOS()
        {
            return this.isMacOS.Value;
        }

        /// <inheritdoc/>
        public bool IsMacOSBrokerAvailable()
        {
            return this.isMacOSBrokerAvailable.Value;
        }

        private bool CheckMacOS()
        {
            this.logger.LogTrace($"IsMacOS: RuntimeInformation.IsOSPlatform(OSPlatform.OSX) = {RuntimeInformation.IsOSPlatform(OSPlatform.OSX)}");
            return RuntimeInformation.IsOSPlatform(OSPlatform.OSX);
        }

        /// <summary>
        /// Minimum Company Portal release number required for unsigned app broker support.
        /// CP 2603 added redirect_uri validation fix for msauth.com.msauth.unsignedapp://auth.
        /// </summary>
        private const int MinimumCPRelease = 2603;

        /// <summary>
        /// Path where Company Portal is expected to be installed on macOS.
        /// </summary>
        public const string CompanyPortalAppPath = "/Applications/Company Portal.app";

        private bool CheckMacOSBrokerAvailable()
        {
            if (!this.IsMacOS())
            {
                return false;
            }

            this.logger.LogTrace($"Checking for Company Portal at: {CompanyPortalAppPath}");

            if (!Directory.Exists(CompanyPortalAppPath))
            {
                this.logger.LogDebug($"macOS broker unavailable: Company Portal not found at {CompanyPortalAppPath}");
                return false;
            }

            this.logger.LogTrace($"Company Portal found at: {CompanyPortalAppPath}");

            try
            {
                var plistPath = $"{CompanyPortalAppPath}/Contents/Info";
                var psi = new ProcessStartInfo
                {
                    FileName = "defaults",
                    Arguments = $"read \"{plistPath}\" CFBundleShortVersionString",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                };

                this.logger.LogTrace($"Reading CP version: defaults read \"{plistPath}\" CFBundleShortVersionString");

                using var process = Process.Start(psi);
                var output = process.StandardOutput.ReadToEnd().Trim();
                var stderr = process.StandardError.ReadToEnd().Trim();
                process.WaitForExit(5000);

                this.logger.LogTrace($"CP version raw output: '{output}'");
                if (!string.IsNullOrEmpty(stderr))
                {
                    this.logger.LogTrace($"CP version stderr: '{stderr}'");
                }

                // Version format: "5.RRRR.B" where RRRR is the release number (e.g., 2603)
                var parts = output.Split('.');
                if (parts.Length >= 2 && int.TryParse(parts[1], out int releaseNumber))
                {
                    var meetsMinimum = releaseNumber >= MinimumCPRelease;
                    this.logger.LogDebug($"Company Portal version: {output} (release {releaseNumber}), minimum required: {MinimumCPRelease}, meets minimum: {meetsMinimum}");
                    this.logger.LogTrace($"Company Portal path: {CompanyPortalAppPath}");
                    this.logger.LogTrace($"Company Portal version parts: major={parts[0]}, release={parts[1]}{(parts.Length >= 3 ? $", build={parts[2]}" : string.Empty)}");

                    if (!meetsMinimum)
                    {
                        this.logger.LogWarning($"macOS broker unavailable: Company Portal {output} (at {CompanyPortalAppPath}) is below minimum required release {MinimumCPRelease}.");
                        return false;
                    }

                    // Check if the Enterprise SSO Extension is registered via MDM.
                    // This is a soft check — disable by setting AZUREAUTH_SKIP_SSO_CHECK=1
                    // if it proves unnecessary (e.g., broker works without it in some configs).
                    if (!this.IsSSOExtensionRegistered())
                    {
                        return false;
                    }

                    return true;
                }

                this.logger.LogDebug($"macOS broker: unable to parse Company Portal version '{output}' from {CompanyPortalAppPath}");
                return false;
            }
            catch (Exception ex)
            {
                this.logger.LogDebug($"macOS broker: failed to check Company Portal version at {CompanyPortalAppPath}: {ex.Message}");
                this.logger.LogTrace($"macOS broker: version check exception: {ex}");
                return false;
            }
        }

        /// <summary>
        /// Checks if the macOS Enterprise SSO Extension is registered via MDM.
        /// Uses `app-sso -l` which returns a plist array of registered extensions.
        /// An empty array means no SSO extensions are configured.
        ///
        /// To skip this check (e.g., if broker works without it), set AZUREAUTH_SKIP_SSO_CHECK=1.
        /// </summary>
        private bool IsSSOExtensionRegistered()
        {
            if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("AZUREAUTH_SKIP_SSO_CHECK")))
            {
                this.logger.LogDebug("SSO Extension check skipped (AZUREAUTH_SKIP_SSO_CHECK is set)");
                return true;
            }

            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "app-sso",
                    Arguments = "-l",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                };

                this.logger.LogTrace("Checking SSO Extension registration: app-sso -l");

                using var process = Process.Start(psi);
                var output = process.StandardOutput.ReadToEnd().Trim();
                process.WaitForExit(5000);

                this.logger.LogTrace($"app-sso output: '{output}'");

                // An empty plist array looks like: <array/> or <array>\n</array>
                bool hasExtensions = !string.IsNullOrEmpty(output)
                    && !output.Contains("<array/>")
                    && !output.Contains("<array>\n</array>");

                if (!hasExtensions)
                {
                    this.logger.LogWarning(
                        "macOS broker unavailable: No Enterprise SSO Extensions registered. " +
                        "Your MDM profile may not have been applied yet. " +
                        "Try restarting or contact your IT admin. " +
                        "Set AZUREAUTH_SKIP_SSO_CHECK=1 to bypass this check.");
                }
                else
                {
                    this.logger.LogDebug("Enterprise SSO Extension is registered");
                }

                return hasExtensions;
            }
            catch (Exception ex)
            {
                // If app-sso isn't available or fails, don't block — assume it might work.
                this.logger.LogDebug($"SSO Extension check failed (non-fatal, proceeding): {ex.Message}");
                return true;
            }
        }

        private bool CheckWindows()
        {
            this.logger.LogTrace($"IsWindows: RuntimeInformation.IsOSPlatform(OSPlatform.Windows) = {RuntimeInformation.IsOSPlatform(OSPlatform.Windows)}");
            return RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
        }

        private bool CheckWindows10()
        {
            if (!this.IsWindows())
            {
                return false;
            }

            var os = Environment.OSVersion;
            var isWin10 = os.Version.Major == 10 && os.Version.Minor == 0;
            this.logger.LogTrace($"IsWindows10: {isWin10}");
            return isWin10;
        }
    }
}
