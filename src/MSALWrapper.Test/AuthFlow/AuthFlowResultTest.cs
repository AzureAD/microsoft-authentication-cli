// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Authentication.MSALWrapper.Test
{
    using System;
    using System.Collections.Generic;

    using FluentAssertions;

    using Microsoft.Authentication.MSALWrapper;
    using Microsoft.IdentityModel.JsonWebTokens;

    using NUnit.Framework;

    internal class AuthFlowResultTest
    {
        public const string FakeToken = "eyJhbGciOiJSUzI1NiIsInR5cCI6IkpXVCIsInJoIjoieHh4IiwieDV0IjoieHh4Iiwia2lkIjoieHh4In0.eyJzdWIiOiIxMjM0NTY3ODkwIiwibmFtZSI6IkpvaG4gRG9lIiwiYXVkIjoiMTExMTExMTEtMTExMS0xMTExLTExMTEtMTExMTExMTExMTExIiwiaWF0IjoxNjE3NjY0Mjc2LCJuYmYiOjE2MTc2NjQyNzYsImV4cCI6MTYxNzY2ODE3NiwiYWNyIjoiMSIsImFpbyI6IllTQjBiM1JoYkd4NUlHWmhhMlVnYTJWNUlDTWtKVjQ9Iiwic2NwIjoidXNlcl9pbXBlcnNvbmF0aW9uIiwidW5pcXVlX25hbWUiOiJreXJhZGVyQG1pY3Jvc29mdC5jb20iLCJ1cG4iOiJreXJhZGVyQG1pY3Jvc29mdC5jb20iLCJ2ZXIiOiIxLjAifQ.bNc3QlL4zIClzFqH68A4hxsR7K-jabQvzB2EodgujQqc0RND_VLVkk2h3iDy8so3azN-964c2z5AiBGY6PVtWKYB-h0Z_VnzbebhDjzPLspEsANyQxaDX_ugOrf7BerQOtILWT5Vqs-A3745Bh0eTDFZpobmeENpANNhRE-yKwScjU8BDY9RimdrA2Z00V0lSliUQwnovWmtfdlbEpWObSFQAK7wCcNnUesV-jNZAUMrDkmTItPA9Z1Ks3NUbqdqMP3D6n99sy8DxQeFmbNQGYocYqI7QH24oNXODq0XB-2zpvCqy4T2jiBLgN_XEaZ5zTzEOzztpgMIWH1AUvEIyw";

        [Test]
        public void ConstructorWithNullAuthFlowName_ThrowsNullArgumentException()
        {
            var tokenResult = new TokenResult(new JsonWebToken(FakeToken), Guid.NewGuid());
            var errors = new List<Exception>();
            Action authFlowResult = () => new AuthFlowResult(tokenResult, errors, null);

            authFlowResult.Should().Throw<ArgumentNullException>();
        }

        [Test]
        public void AuthFlowName_Cannot_Be_Empty()
        {
            var tokenResult = new TokenResult(new JsonWebToken(FakeToken), Guid.NewGuid());
            var errors = new List<Exception>();
            Action authFlowResult = () => new AuthFlowResult(tokenResult, errors, string.Empty);

            authFlowResult.Should().Throw<ArgumentException>().WithMessage("Param 'authFlowName' cannot be empty");
        }

        [Test]
        public void ConstructorWithNonNullArgs()
        {
            var tokenResult = new TokenResult(new JsonWebToken(FakeToken), Guid.NewGuid());
            var errors = new List<Exception>();
            var authFlowName = "AuthFlowName";
            AuthFlowResult subject = new AuthFlowResult(tokenResult, errors, authFlowName);

            subject.Success.Should().BeTrue();
            subject.TokenResult.Should().Be(tokenResult);
            subject.Errors.Should().BeEmpty();
        }

        [Test]
        public void AddErrors()
        {
            var subject = new AuthFlowResult(null, null, "TestingAddErrors");
            var errors = new List<Exception>
            {
                new ArgumentException("something can't be null"),
                new NullReferenceException("you cannot get there from here"),
            };

            subject.AddErrors(errors);
            subject.Errors.Should().BeEquivalentTo(errors);
        }
    }
}
