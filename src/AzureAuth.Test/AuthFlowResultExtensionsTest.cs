// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace AzureAuth.Test
{
    using System;

    using FluentAssertions;

    using Microsoft.Authentication.AzureAuth;
    using Microsoft.Authentication.MSALWrapper;
    using Microsoft.Identity.Client;
    using Microsoft.IdentityModel.JsonWebTokens;

    using NUnit.Framework;

    public class AuthFlowResultExtensionsTest
    {
        private const string FakeToken = "eyJhbGciOiJSUzI1NiIsInR5cCI6IkpXVCIsInJoIjoieHh4IiwieDV0IjoieHh4Iiwia2lkIjoieHh4In0.eyJzdWIiOiIxMjM0NTY3ODkwIiwibmFtZSI6IkpvaG4gRG9lIiwiYXVkIjoiMTExMTExMTEtMTExMS0xMTExLTExMTEtMTExMTExMTExMTExIiwiaWF0IjoxNjE3NjY0Mjc2LCJuYmYiOjE2MTc2NjQyNzYsImV4cCI6MTYxNzY2ODE3NiwiYWNyIjoiMSIsImFpbyI6IllTQjBiM1JoYkd4NUlHWmhhMlVnYTJWNUlDTWtKVjQ9Iiwic2NwIjoidXNlcl9pbXBlcnNvbmF0aW9uIiwidW5pcXVlX25hbWUiOiJreXJhZGVyQG1pY3Jvc29mdC5jb20iLCJ1cG4iOiJreXJhZGVyQG1pY3Jvc29mdC5jb20iLCJ2ZXIiOiIxLjAifQ.bNc3QlL4zIClzFqH68A4hxsR7K-jabQvzB2EodgujQqc0RND_VLVkk2h3iDy8so3azN-964c2z5AiBGY6PVtWKYB-h0Z_VnzbebhDjzPLspEsANyQxaDX_ugOrf7BerQOtILWT5Vqs-A3745Bh0eTDFZpobmeENpANNhRE-yKwScjU8BDY9RimdrA2Z00V0lSliUQwnovWmtfdlbEpWObSFQAK7wCcNnUesV-jNZAUMrDkmTItPA9Z1Ks3NUbqdqMP3D6n99sy8DxQeFmbNQGYocYqI7QH24oNXODq0XB-2zpvCqy4T2jiBLgN_XEaZ5zTzEOzztpgMIWH1AUvEIyw";

        [Test]
        public void Event_From_Null_AuthFlowResult()
        {
            // Arrange
            AuthFlowResult subject = null;

            // Act
            var eventData = subject.EventData();

            // Assert
            eventData.Should().BeNull();
        }

        [Test]
        public void Event_From_AuthFlowResult_With_Null_TokenResult_Null_Errors()
        {
            // Arrange
            AuthFlowResult subject = new AuthFlowResult(null, null, "AuthFlowName");

            // Act
            var eventData = subject.EventData();

            // Assert
            eventData.Properties.Should().NotContainKey("msal_correlation_ids");
            eventData.Properties.Should().NotContainKey("error_messages");
            eventData.Measures.Should().NotContainKey("token_validity_hours");
            eventData.Properties.Should().NotContainKey("silent");

            eventData.Properties.Should().Contain("authflow", "AuthFlowName");
            eventData.Properties.Should().Contain("success", "False");
            eventData.Measures.Should().ContainKey("duration_milliseconds");
        }

        /// <summary>
        /// Test to generate event data from an authflow result with null token result and some errors.
        /// </summary>
        [Test]
        public void TestGenerateEvent_From_AuthFlowResult_With_Errors_And_Null_TokenResult()
        {
            // Arrange
            var errors = new[]
            {
                new Exception("Exception 1."),
            };

            AuthFlowResult subject = new AuthFlowResult(null, errors, "AuthFlowName");

            // Act
            var eventData = subject.EventData();

            // Assert
            eventData.Properties.Should().NotContainKey("msal_correlation_ids");
            eventData.Measures.Should().NotContainKey("token_validity_minutes");
            eventData.Properties.Should().NotContainKey("silent");
            eventData.Properties.Should().Contain("authflow", "AuthFlowName");
            eventData.Properties.Should().Contain("success", "False");
            eventData.Properties.Should().Contain("error_messages", "[{\"Message\":\"Exception 1.\",\"InnerException\":null,\"ExceptionType\":\"System.Exception\",\"AADErrorCode\":null,\"CorrelationId\":null}]");
            eventData.Measures.Should().ContainKey("duration_milliseconds");
        }

        /// <summary>
        /// Test to generate event data from an authflow result with token result and msal errors.
        /// </summary>
        [Test]
        public void Event_From_AuthFlowResult_With_MsalErrors_And_TokenResult()
        {
            // Arrange
            var correlationID1 = Guid.NewGuid().ToString();
            var msalServiceException = new MsalServiceException("1", "An MSAL Service Exception message");
            msalServiceException.CorrelationId = correlationID1;

            var msalUIRequiredException = new MsalUiRequiredException("2", "An MSAL UI Required Exception message");
            msalUIRequiredException.CorrelationId = null;

            var errors = new[]
            {
                msalServiceException,
                msalUIRequiredException,
            };

            var tokenResultCorrelationID = Guid.NewGuid();
            var tokenResult = new TokenResult(new JsonWebToken(FakeToken), tokenResultCorrelationID);

            AuthFlowResult subject = new AuthFlowResult(tokenResult, errors, "AuthFlowName");

            // Act
            var eventData = subject.EventData();

            // Assert
            eventData.Properties.Should().Contain("authflow", "AuthFlowName");
            eventData.Properties.Should().Contain("success", "True");
            eventData.Properties.Should().Contain("msal_correlation_id", $"{tokenResultCorrelationID}");
            eventData.Properties.Should().Contain("silent", "False");
            eventData.Properties.Should().Contain("error_messages", "[{\"Message\":\"An MSAL Service Exception message\",\"InnerException\":null,\"ExceptionType\":\"Microsoft.Identity.Client.MsalServiceException\",\"AADErrorCode\":\"AADSTS1\",\"CorrelationId\":\"" + $"{correlationID1}" + "\"},{\"Message\":\"An MSAL UI Required Exception message\",\"InnerException\":null,\"ExceptionType\":\"Microsoft.Identity.Client.MsalUiRequiredException\",\"AADErrorCode\":\"AADSTS2\",\"CorrelationId\":null}]");
            eventData.Measures.Should().ContainKey("token_validity_minutes");
            eventData.Measures.Should().ContainKey("duration_milliseconds");
        }

        [Test]
        public void Event_From_AuthFlowResult_With_TokenResult_And_Null_Errors()
        {
            // Arrange
            var tokenResultCorrelationID = Guid.NewGuid();
            var tokenResult = new TokenResult(new JsonWebToken(FakeToken), tokenResultCorrelationID);

            AuthFlowResult subject = new AuthFlowResult(tokenResult, null, "AuthFlowName");

            var expectedCorrelationIDs = $"{tokenResultCorrelationID}";

            // Act
            var eventData = subject.EventData();

            // Assert
            eventData.Properties.Should().NotContainKey("error_messages");
            eventData.Properties.Should().Contain("authflow", "AuthFlowName");
            eventData.Properties.Should().Contain("success", "True");
            eventData.Properties.Should().Contain("msal_correlation_id", expectedCorrelationIDs);
            eventData.Properties.Should().Contain("silent", "False");
            eventData.Measures.Should().ContainKey("token_validity_minutes");
            eventData.Measures.Should().ContainKey("duration_milliseconds");
        }
    }
}
