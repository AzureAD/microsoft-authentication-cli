// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace AdoPat.Test
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using FluentAssertions;
    using FluentAssertions.Extensions;
    using Microsoft.Authentication.AdoPat;
    using Microsoft.VisualStudio.Services.DelegatedAuthorization;
    using NUnit.Framework;

    public class PatTokenExtensionsTest
    {
        // The PatToken values in this test intentionally match those given at
        // https://learn.microsoft.com/en-us/rest/api/azure/devops/tokens/pats/create?view=azure-devops-rest-7.1&tabs=HTTP#pattoken
        // right down to the fractional second values in the timestamps. This test is intended to ensure
        // parity between the exact REST API response and our serialization of the VisualStudio Services
        // PatToken type. If this test is ever failing, refer to the API documentation as a source of truth.
        [Test]
        public void AsJson_Formats_PatToken()
        {
            // Arrange
            var token = new PatToken
            {
                DisplayName = "new_token",
                Scope = "app_token",
                TargetAccounts = new List<Guid> { new Guid("38aaa865-2c70-4bf7-a308-0c6539c38c1a") },
                ValidTo = new DateTime(2020, 12, 1, 23, 46, 23, DateTimeKind.Utc).AddMilliseconds(320.0),
                ValidFrom = new DateTime(2020, 11, 2, 22, 56, 52, DateTimeKind.Utc).AddNanoseconds(103_333_300),
                AuthorizationId = new Guid("4ab5764f-4193-4f1d-b995-64144880b7d7"),
                Token = "dip55dwf4vpitomw63jzvomefmi2jluguprzwwqwuc6xq4fhocwq",
            };

            var expected = string.Join(
                Environment.NewLine,
                "{",
                "  \"displayName\": \"new_token\",",
                "  \"validTo\": \"2020-12-01T23:46:23.32Z\",",
                "  \"scope\": \"app_token\",",
                "  \"targetAccounts\": [",
                "    \"38aaa865-2c70-4bf7-a308-0c6539c38c1a\"",
                "  ],",
                "  \"validFrom\": \"2020-11-02T22:56:52.1033333Z\",",
                "  \"authorizationId\": \"4ab5764f-4193-4f1d-b995-64144880b7d7\",",
                "  \"token\": \"dip55dwf4vpitomw63jzvomefmi2jluguprzwwqwuc6xq4fhocwq\"",
                "}");

            // Act
            var subject = token.AsJson();

            // Assert
            subject.Should().BeEquivalentTo(expected);
        }
    }
}
