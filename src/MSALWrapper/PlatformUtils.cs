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

        private const string CompanyPortalPath = "/Applications/Company Portal.app";

        private bool CheckMacOSBrokerAvailable()
        {
            if (!this.IsMacOS())
            {
                return false;
            }

            if (!Directory.Exists(CompanyPortalPath))
            {
                this.logger.LogDebug("macOS broker unavailable: Company Portal not installed");
                return false;
            }

            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "defaults",
                    Arguments = $"read \"{CompanyPortalPath}/Contents/Info\" CFBundleShortVersionString",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                };

                using var process = Process.Start(psi);
                var output = process.StandardOutput.ReadToEnd().Trim();
                process.WaitForExit(5000);

                // Version format: "5.RRRR.B" where RRRR is the release number (e.g., 2603)
                var parts = output.Split('.');
                if (parts.Length >= 2 && int.TryParse(parts[1], out int releaseNumber))
                {
                    var meetsMinimum = releaseNumber >= MinimumCPRelease;
                    this.logger.LogDebug($"Company Portal version: {output}, release: {releaseNumber}, meets minimum ({MinimumCPRelease}): {meetsMinimum}");

                    if (!meetsMinimum)
                    {
                        this.logger.LogWarning($"macOS broker unavailable: Company Portal version {output} is below minimum required release {MinimumCPRelease}. Falling back to web auth.");
                    }

                    return meetsMinimum;
                }

                this.logger.LogDebug($"macOS broker: unable to parse Company Portal version '{output}'");
                return false;
            }
            catch (Exception ex)
            {
                this.logger.LogDebug($"macOS broker: failed to check Company Portal version: {ex.Message}");
                return false;
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
