// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Authentication.MSALWrapper.Test
{
    using FluentAssertions;
    using Microsoft.Authentication.MSALWrapper;
    using Microsoft.IdentityModel.JsonWebTokens;
    using NUnit.Framework;

    public class TokenResultTest
    {
        public const string FakeToken = "eyJhbGciOiJSUzI1NiIsInR5cCI6IkpXVCIsInJoIjoieHh4IiwieDV0IjoieHh4Iiwia2lkIjoieHh4In0.eyJzdWIiOiIxMjM0NTY3ODkwIiwibmFtZSI6IkpvaG4gRG9lIiwiYXVkIjoiMTExMTExMTEtMTExMS0xMTExLTExMTEtMTExMTExMTExMTExIiwiaWF0IjoxNjE3NjY0Mjc2LCJuYmYiOjE2MTc2NjQyNzYsImV4cCI6MTYxNzY2ODE3NiwiYWNyIjoiMSIsImFpbyI6IllTQjBiM1JoYkd4NUlHWmhhMlVnYTJWNUlDTWtKVjQ9Iiwic2NwIjoidXNlcl9pbXBlcnNvbmF0aW9uIiwidW5pcXVlX25hbWUiOiJreXJhZGVyQG1pY3Jvc29mdC5jb20iLCJ1cG4iOiJreXJhZGVyQG1pY3Jvc29mdC5jb20iLCJ2ZXIiOiIxLjAifQ.bNc3QlL4zIClzFqH68A4hxsR7K-jabQvzB2EodgujQqc0RND_VLVkk2h3iDy8so3azN-964c2z5AiBGY6PVtWKYB-h0Z_VnzbebhDjzPLspEsANyQxaDX_ugOrf7BerQOtILWT5Vqs-A3745Bh0eTDFZpobmeENpANNhRE-yKwScjU8BDY9RimdrA2Z00V0lSliUQwnovWmtfdlbEpWObSFQAK7wCcNnUesV-jNZAUMrDkmTItPA9Z1Ks3NUbqdqMP3D6n99sy8DxQeFmbNQGYocYqI7QH24oNXODq0XB-2zpvCqy4T2jiBLgN_XEaZ5zTzEOzztpgMIWH1AUvEIyw";

        private JsonWebToken jwt;
        private TokenResult subject;

        [SetUp]
        public void Setup()
        {
            this.jwt = new JsonWebToken(FakeToken);
            this.subject = new TokenResult(this.jwt);
        }

        [Test]
        public void ConstructorTakesJWT()
        {
            this.subject.JWT.Should().Be(this.jwt);
            this.subject.Token.Should().Be(FakeToken);
            this.subject.User.Should().Be("kyrader@microsoft.com");
            this.subject.ValidFor.Should().NotBeCloseTo(new System.TimeSpan(0, 0, 0), new System.TimeSpan(0, 1, 0));
        }

        [Test]
        public void TokenResult_ToString()
        {
            this.subject.ToString().Should().Be("Token cache warm for kyrader@microsoft.com (John Doe)");
        }

        [Test]
        public void TokenResult_DisplayName()
        {
            this.subject.DisplayName.Should().Be("John Doe");
        }

        [Test]
        public void TokenResult_ToJson()
        {
            var expected = $@"{{
    ""user"": ""kyrader@microsoft.com"",
    ""display_name"": ""John Doe"",
    ""token"": ""{FakeToken}"",
    ""expiration_date"": ""1617668176""
}}";
            this.subject.ToJson().Should().Be(expected);
        }
    }
}
