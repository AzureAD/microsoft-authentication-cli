// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Authentication.MSALWrapper
{
    using System;
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

        /// <summary>
        /// Initializes a new instance of the <see cref="PlatformUtils"/> class.
        /// </summary>
        /// <param name="logger">An <see cref="ILogger"/> to use.</param>
        public PlatformUtils(ILogger logger)
        {
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.isWindows = new Lazy<bool>(() => this.CheckWindows());
            this.isWindows10 = new Lazy<bool>(() => this.CheckWindows10());
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
