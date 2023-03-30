// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Authentication.AdoPat
{
    using System.Text.Json;
    using Microsoft.VisualStudio.Services.DelegatedAuthorization;

    /// <summary>
    /// <see cref="PatToken"/> extension methods.
    /// </summary>
    public static class PatTokenExtensions
    {
        private static readonly JsonSerializerOptions JsonSerializerOptions = new JsonSerializerOptions
        {
            // PatToken's fields are PascalCase, but the Azure DevOps REST returns camelCase names.
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,

            // We output pretty-printed JSON for human readability.
            WriteIndented = true,
        };

        /// <summary>
        /// Format a <see cref="PatToken"/> as JSON.
        /// </summary>
        /// <param name="token">A <see cref="PatToken"/>.</param>
        /// <returns>The JSON representation of a <see cref="PatToken"/> as a <see cref="string"/>.</returns>
        public static string AsJson(this PatToken token)
        {
            return JsonSerializer.Serialize(token, JsonSerializerOptions);
        }
    }
}
