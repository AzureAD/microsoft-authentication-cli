// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Authentication.AzureAuth
{
    using System;
    using System.Collections.Generic;
    using System.IO.Abstractions;
    using Tomlyn;

    /// <summary>
    /// A config contains a collection of <see cref="Alias"/>.
    /// </summary>
    public class Config
    {
        /// <summary>
        /// Gets or sets the alias.
        /// </summary>
        public Dictionary<string, Alias> Alias { get; set; }

        /// <summary>
        /// Create a Config instance from <see cref="Toml"/> format file.
        /// </summary>
        /// <param name="configFile">
        /// The full path of config file.
        /// </param>
        /// <param name="fileSystem">
        /// The file system.
        /// </param>
        /// <returns>
        /// The <see cref="Config"/>.
        /// </returns>
        /// <exception cref="ArgumentException">
        /// The argument exception.
        /// </exception>
        public static Config FromFile(string configFile, IFileSystem fileSystem)
        {
            // Requiring a full path feels silly because we can only get runtime errors
            // here if we get it wrong, not compile time errors.
            if (!fileSystem.Path.IsPathRooted(configFile))
            {
                throw new ArgumentException($"configFile '${configFile}' must be a full path", nameof(configFile));
            }

            string text = fileSystem.File.ReadAllText(configFile);
            return Toml.ToModel<Config>(text);
        }
    }
}
