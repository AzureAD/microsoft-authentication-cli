// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace AzureAuth.Test
{
    using FluentAssertions;

    using Microsoft.Authentication.AzureAuth;

    using NUnit.Framework;

    public class PromptHintTest
    {
        [Test]
        public void Prompts_Are_Prefixed()
        {
            string prompt = "Test Prompt Hint";

            PromptHint.Prefixed(prompt)
                .Should()
                .BeEquivalentTo($"{PromptHint.AzureAuth}: {prompt}");
        }

        [Test]
        public void No_Prompt_Is_Just_AzureAuth()
        {
            string prompt = string.Empty;

            PromptHint.Prefixed(prompt)
                .Should()
                .BeEquivalentTo(PromptHint.AzureAuth);
        }
    }
}
