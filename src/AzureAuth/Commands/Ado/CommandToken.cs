// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Authentication.AzureAuth.Commands.Ado
{
    using System;
    using System.Collections.Generic;
    using System.IO;

    using McMaster.Extensions.CommandLineUtils;

    using Microsoft.Authentication.AzureAuth.Ado;
    using Microsoft.Authentication.MSALWrapper;
    using Microsoft.Extensions.Logging;
    using Microsoft.Office.Lasso.Interfaces;
    using Microsoft.Office.Lasso.Telemetry;

    using NLog;

    /// <summary>
    /// ADO Command for using either an ADO PAT or acquiring an AAD Access Token.
    /// </summary>
    [Command("token", Description = @"Get a PAT from env vars or an AAD AT for Azure Devops.
For use by short-lived processes. More info at https://aka.ms/AzureAuth")]
    public class CommandToken
    {
        private const string OutputOption = "--output";
        private const string OutputOptionDescription = "How to print the token. One of [token, header, headervalue].\nDefault: token";

        /// <summary>
        /// The available Token Formats.
        /// </summary>
        public enum OutputMode
        {
            /// <summary>
            /// Raw Token
            /// </summary>
            Token,

            /// <summary>
            /// Authorization http header.
            /// </summary>
            Header,

            /// <summary> Authorization http header - Value Only </summary>
            HeaderValue,
        }

        [Option(OutputOption, OutputOptionDescription, CommandOptionType.SingleValue)]
        private OutputMode Output { get; set; } = OutputMode.Token;

        [Option(CommandAad.TenantOption, Description = "The Azure Tenant ID to use for authentication. Defaults to Microsoft.")]
        private string Tenant { get; set; } = AzureAuth.Ado.Constants.Tenant.Microsoft;

        [Option(CommandAad.ModeOption, CommandAad.AuthModeHelperText, CommandOptionType.MultipleValue)]
        private IEnumerable<AuthMode> AuthModes { get; set; } = new[] { AuthMode.Default };

        [Option(CommandAad.DomainOption, Description = CommandAad.DomainHelpText)]
        private string Domain { get; set; }

        [Option(CommandAad.TimeoutOption, CommandAad.TimeoutHelpText, CommandOptionType.SingleValue)]
        private double Timeout { get; set; } = CommandAad.GlobalTimeout.TotalMinutes;

        [Option(CommandAad.PromptHintOption, CommandAad.PromptHintHelpText, CommandOptionType.SingleValue)]
        private string PromptHint { get; set; }

        /// <summary>
        /// Format a PAT based on <paramref name="output"/>.
        /// </summary>
        /// <param name="value">The PAT value.</param>
        /// <param name="output">The output mode.</param>
        /// <param name="scheme">The <see cref="Authorization"/> scheme to use.</param>
        /// <returns>The formatted PAT value ready for printing.</returns>
        public static string FormatToken(string value, OutputMode output, Authorization scheme) => output switch
        {
            OutputMode.Token => value,
            OutputMode.Header => value.AsHeader(scheme),
            OutputMode.HeaderValue => value.AsHeaderValue(scheme),
            _ => throw new ArgumentOutOfRangeException(nameof(scheme)),
        };

        /// <summary>
        /// Executes the command and returns a status code indicating the success or failure of the execution.
        /// </summary>
        /// <param name="logger">The <see cref="ILogger{T}"/> instance that is used for logging.</param>
        /// <param name="env">An <see cref="IEnv"/> to use.</param>
        /// <param name="eventData">Lasso injected command event data.</param>
        /// <returns>An integer status code. 0 for success and non-zero for failure.</returns>
        public int OnExecute(ILogger<CommandToken> logger, IEnv env, CommandExecuteEventData eventData)
        {
            // First attempt using a PAT.
            var pat = PatFromEnv.Get(env);
            if (pat.Exists)
            {
                logger.LogDebug($"Using PAT from env var {pat.EnvVarSource}");
                logger.LogInformation(FormatToken(pat.Value, this.Output, Authorization.Basic));
                return 0;
            }

            // If no PAT then use AAD AT.
            var authResult = AzureAuth.Ado.TokenFetcher.AccessToken(
                logger: logger,
                mode: this.AuthModes.Combine().PreventInteractionIfNeeded(env),
                domain: this.Domain,
                prompt: AzureAuth.PromptHint.Prefixed(this.PromptHint),
                timeout: TimeSpan.FromMinutes(this.Timeout));

            var authflow = authResult.Success;
            if (authflow != null)
            {
                logger.LogDebug($"Acquired AAD AT via {authflow.AuthFlowName} in {authflow.Duration.TotalSeconds:0.00} sec");
                logger.LogInformation(FormatToken(authflow.TokenResult.Token, this.Output, Authorization.Bearer));
                return 0;
            }

            logger.LogError($"Failed to find a PAT and authenticate to ADO.");
            foreach (var attempt in authResult.Attempts)
            {
                logger.LogError($"{attempt.AuthFlowName} failed after {attempt.Duration.TotalSeconds:0.00} sec. Error count: {attempt.Errors.Count}");
                foreach (var e in attempt.Errors)
                {
                    logger.LogError($"  {e.Message}");
                }
            }

            return 1;
        }
    }
}
