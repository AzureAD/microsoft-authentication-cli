// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Authentication.AzureAuth.Commands.Ado
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using McMaster.Extensions.CommandLineUtils;

    using Microsoft.Authentication.AzureAuth.Ado;
    using Microsoft.Authentication.MSALWrapper;
    using Microsoft.Extensions.Logging;
    using Microsoft.Office.Lasso.Interfaces;
    using Microsoft.Office.Lasso.Telemetry;

    /// <summary>
    /// ADO Command for using either an ADO PAT or acquiring an AAD Access Token.
    /// </summary>
    [Command("token", Description = @"Get a PAT from env vars or an AAD AT for Azure Devops.
For use by short-lived processes. More info at https://aka.ms/AzureAuth")]
    public class CommandToken
    {
        private const string OutputOption = "--output";
        private const string OutputOptionDescription = "How to print the token. One of [token, header, headervalue].\nDefault: token";

        private const string DomainOptionDescription = CommandAad.DomainHelpText + "\n[default: " + AzureAuth.Ado.Constants.PreferredDomain + "]";

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
        private IEnumerable<AuthMode> AuthModes { get; set; }

        [Option(CommandAad.DomainOption, Description = DomainOptionDescription)]
        private string Domain { get; set; } = AzureAuth.Ado.Constants.PreferredDomain;

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
        /// <param name="telemetryService">An <see cref="ITelemetryService"/>.</param>
        /// <param name="publicClientAuth">An <see cref="IPublicClientAuth"/>.</param>
        /// <param name="eventData">Lasso injected command event data.</param>
        /// <returns>An integer status code. 0 for success and non-zero for failure.</returns>
        public int OnExecute(ILogger<CommandToken> logger, IEnv env, ITelemetryService telemetryService, IPublicClientAuth publicClientAuth, CommandExecuteEventData eventData)
        {
            // Always check AZUREAUTH_ADO_PAT first - this is an explicit user override.
            var adoPat = env.Get(EnvVars.AdoPat);
            if (!string.IsNullOrEmpty(adoPat))
            {
                logger.LogDebug($"Using PAT from env var {EnvVars.AdoPat}");
                logger.LogInformation(FormatToken(adoPat, this.Output, Authorization.Basic));
                return 0;
            }

            // Check if we're in an ADO Pipeline environment.
            bool isAdoPipeline = env.IsAdoPipeline();
            var systemAccessToken = env.Get(EnvVars.SystemAccessToken);

            if (isAdoPipeline)
            {
                if (!string.IsNullOrEmpty(systemAccessToken))
                {
                    logger.LogDebug($"Using token from env var {EnvVars.SystemAccessToken}");
                    logger.LogInformation(FormatToken(systemAccessToken, this.Output, Authorization.Basic));
                    return 0;
                }
                else
                {
                    logger.LogError(
                        $"Running in an Azure DevOps Pipeline environment but {EnvVars.SystemAccessToken} is not set. "
                        + "Interactive authentication is not possible in a pipeline. "
                        + "Ensure the pipeline has access to the system token.");
                    return 1;
                }
            }
            else if (!string.IsNullOrEmpty(systemAccessToken))
            {
                logger.LogWarning(
                    $"{EnvVars.SystemAccessToken} is set but this does not appear to be an Azure DevOps Pipeline environment. "
                    + "Having this variable set on a developer machine is unusual. It will be ignored.");
            }

            // If command line options for mode are not specified, then use the environment variables.
            this.AuthModes ??= env.ReadAuthModeFromEnvOrSetDefault();
            if (!this.AuthModes.Any())
            {
                logger.LogError($"Invalid value specified for environment variable {EnvVars.AuthMode}. Allowed values are: {CommandAad.AuthModeAllowedValues}");
                return 1;
            }

            // If no PAT then use AAD AT.
            TokenResult token = publicClientAuth.Token(
                AzureAuth.Ado.AuthParameters.AdoParameters(this.Tenant),
                authModes: this.AuthModes,
                domain: this.Domain,
                prompt: this.PromptHint,
                timeout: TimeSpan.FromMinutes(this.Timeout),
                eventData);

            if (token == null)
            {
                logger.LogError($"Failed to find a PAT and authenticate to ADO.");
                return 1;
            }

            // Do not use logger to avoid printing tokens into log files.
            Console.WriteLine(FormatToken(token.Token, this.Output, Authorization.Bearer));
            return 0;
        }
    }
}
