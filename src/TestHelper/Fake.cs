// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace AzureAuth.Test
{
    using System;

    public static class Fake
    {
        public const string Domain = "contoso.com";
        public const string Token = "eyJhbGciOiJSUzI1NiIsInR5cCI6IkpXVCIsInJoIjoieHh4IiwieDV0IjoieHh4Iiwia2lkIjoieHh4In0.eyJzdWIiOiIxMjM0NTY3ODkwIiwibmFtZSI6IkpvaG4gRG9lIiwiYXVkIjoiMTExMTExMTEtMTExMS0xMTExLTExMTEtMTExMTExMTExMTExIiwiaWF0IjoxNjE3NjY0Mjc2LCJuYmYiOjE2MTc2NjQyNzYsImV4cCI6MTYxNzY2ODE3NiwiYWNyIjoiMSIsImFpbyI6IllTQjBiM1JoYkd4NUlHWmhhMlVnYTJWNUlDTWtKVjQ9Iiwic2NwIjoidXNlcl9pbXBlcnNvbmF0aW9uIiwidW5pcXVlX25hbWUiOiJreXJhZGVyQG1pY3Jvc29mdC5jb20iLCJ1cG4iOiJreXJhZGVyQG1pY3Jvc29mdC5jb20iLCJ2ZXIiOiIxLjAifQ.bNc3QlL4zIClzFqH68A4hxsR7K-jabQvzB2EodgujQqc0RND_VLVkk2h3iDy8so3azN-964c2z5AiBGY6PVtWKYB-h0Z_VnzbebhDjzPLspEsANyQxaDX_ugOrf7BerQOtILWT5Vqs-A3745Bh0eTDFZpobmeENpANNhRE-yKwScjU8BDY9RimdrA2Z00V0lSliUQwnovWmtfdlbEpWObSFQAK7wCcNnUesV-jNZAUMrDkmTItPA9Z1Ks3NUbqdqMP3D6n99sy8DxQeFmbNQGYocYqI7QH24oNXODq0XB-2zpvCqy4T2jiBLgN_XEaZ5zTzEOzztpgMIWH1AUvEIyw";
        public static readonly Guid Client = new Guid("a7b59161-cd70-46e9-aca5-883f24060eb1");
        public static readonly string Tenant = "a7b59161-cd70-46e9-aca5-883f24060eb2";
        public static readonly Guid Resource = new Guid("a7b59161-cd70-46e9-aca5-883f24060eb3");
        public static readonly string[] Scopes = new[] { $"{Resource}/.default" };
    }
}
