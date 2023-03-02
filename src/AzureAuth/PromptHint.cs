// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Authentication.AzureAuth
{
    /// <summary>
    /// Functions for ensuring a prompt hint is displayed with an AzureAuth prefix.
    /// </summary>
    public static class PromptHint
    {
        /// <summary>
        /// Azure Auth Display name for prompt hints.
        /// </summary>
        public const string AzureAuth = "Azure Auth";

        /// <summary>
        /// Prefix a prompt hint for display.
        /// </summary>
        /// <param name="prompt">Original supplied prompt hint.</param>
        /// <returns>Prefixed prompt hint or our default prefix <see cref="AzureAuth"/>.</returns>
        public static string Prefixed(string prompt)
        {
            if (string.IsNullOrEmpty(prompt))
            {
                return AzureAuth;
            }

            return $"{AzureAuth}: {prompt}";
        }
    }
}
