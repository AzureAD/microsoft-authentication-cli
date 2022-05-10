// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Authentication.MSALWrapper
{
    using System;
    using Microsoft.IdentityModel.JsonWebTokens;

    /// <summary>
    /// The auth type.
    /// </summary>
    public enum AuthType
    {
        /// <summary>
        /// The silent.
        /// </summary>
        Silent,

        /// <summary>
        /// The interactive.
        /// </summary>
        Interactive,

        /// <summary>
        /// The device code flow.
        /// </summary>
        DeviceCodeFlow,
    }

    /// <summary>
    /// The token result.
    /// </summary>
    public class TokenResult
    {
        private static DateTime unixEpochStart = new DateTime(1970, 1, 1);

        private JsonWebToken jwt;

        /// <summary>
        /// Initializes a new instance of the <see cref="TokenResult"/> class.
        /// </summary>
        /// <param name="jwt">The jwt.</param>
        public TokenResult(JsonWebToken jwt)
        {
            this.JWT = jwt;
        }

        /// <summary>
        /// Gets or sets the jwt.
        /// </summary>
        public JsonWebToken JWT
        {
            get => this.jwt;
            set
            {
                this.jwt = value;
                this.Token = this.jwt?.EncodedToken;
                this.User = this.jwt?.GetAzureUserName();
                this.DisplayName = this.jwt?.GetDisplayName();
                this.ValidFor = this.jwt == null ? default(TimeSpan) : (this.jwt.ValidTo - DateTime.UtcNow);
            }
        }

        /// <summary>
        /// Gets the token.
        /// </summary>
        public string Token { get; internal set; }

        /// <summary>
        /// Gets the user.
        /// </summary>
        public string User { get; internal set; }

        /// <summary>
        /// Gets the display name.
        /// </summary>
        public string DisplayName { get; internal set; }

        /// <summary>
        /// Gets the valid for timespan.
        /// </summary>
        public TimeSpan ValidFor { get; internal set; }

        /// <summary>
        /// Gets the auth type.
        /// </summary>
        public AuthType AuthType { get; internal set; }

        /// <summary>
        /// The to string.
        /// </summary>
        /// <returns>The <see cref="string"/>.</returns>
        public override string ToString()
        {
            return $"Token cache warm for {this.User} ({this.DisplayName})";
        }

        /// <summary>
        /// The method to convert to json.
        /// </summary>
        /// <returns>The <see cref="string"/>.</returns>
        public string ToJson()
        {
            var unixTime = this.jwt.ValidTo.Subtract(unixEpochStart).TotalSeconds;

            return $@"{{
    ""user"": ""{this.User}"",
    ""display_name"": ""{this.DisplayName}"",
    ""token"": ""{this.Token}"",
    ""expiration_date"": ""{unixTime}""
}}";
        }
    }
}
