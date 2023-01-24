// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Authentication.MSALWrapper
{
    using System;
    using Microsoft.IdentityModel.JsonWebTokens;

    /// <summary>
    /// Token result.
    /// </summary>
    public class TokenResult
    {
        private JsonWebToken jwt;

        /// <summary>
        /// Initializes a new instance of the <see cref="TokenResult"/> class.
        /// </summary>
        /// <param name="jwt">The jwt.</param>
        /// <param name="correlationID">The correlation ID.</param>
        public TokenResult(JsonWebToken jwt, Guid correlationID)
        {
            this.JWT = jwt;
            this.CorrelationID = correlationID;
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
                this.SID = this.jwt?.GetSID();
            }
        }

        /// <summary>
        /// Gets the correlation ID.
        /// </summary>
        public Guid CorrelationID { get; internal set; }

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
        /// Gets a value indicating whether this token was acquired silently or not.
        /// </summary>
        public bool IsSilent { get; internal set; }

        /// <summary>
        /// Gets the user's security identifier.
        /// </summary>
        public string SID { get; internal set; }

        /// <summary>
        /// To string that shows successful authentication for user.
        /// </summary>
        /// <returns>The <see cref="string"/>.</returns>
        public override string ToString()
        {
            return $"Token cache warm for {this.User} ({this.DisplayName})";
        }

        /// <summary>
        /// Converts the jwt to a string.
        /// </summary>
        /// <returns>The <see cref="string"/>.</returns>
        public string ToJson()
        {
            var unixTime = ((DateTimeOffset)this.jwt.ValidTo).ToUnixTimeSeconds();

            return $@"{{
    ""user"": ""{this.User}"",
    ""display_name"": ""{this.DisplayName}"",
    ""token"": ""{this.Token}"",
    ""expiration_date"": ""{unixTime}""
}}";
        }
    }
}
