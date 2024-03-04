// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Authentication.AzureAuth.Commands.Ado
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.IO;
    using McMaster.Extensions.CommandLineUtils;

    using Microsoft.Authentication.AdoPat;
    using Microsoft.Authentication.AzureAuth.Ado;
    using Microsoft.Authentication.MSALWrapper;
    using Microsoft.Extensions.Logging;
    using Microsoft.Identity.Client.Extensions.Msal;
    using Microsoft.Office.Lasso.Telemetry;
    using Microsoft.VisualStudio.Services.DelegatedAuthorization;
    using Microsoft.VisualStudio.Services.OAuth;
    using Microsoft.VisualStudio.Services.WebApi;

    using PatStorageParameters = Microsoft.Authentication.AzureAuth.Ado.Constants.PatStorageParameters;

    /// <summary>
    /// An ADO Command for creating or fetching, and returning Azure Devops PATs.
    /// </summary>
    [Command("pat", Description = "Create and cache Azure Devops Personal Access Tokens (PATs) using encrypted local storage.")]
    [Subcommand(typeof(Pat.CommandScopes))]
    public class CommandPat
    {
        private const string OrganizationOption = "--organization";
        private const string OrganizationHelp = "The name of the Azure DevOps organization.";

        private const string DisplayNameOption = "--display-name";
        private const string DisplayNameHelp = "The PAT name.";

        private const string ScopeOption = "--scope";
        private const string ScopeHelp = "A token scope for accessing Azure DevOps resources. Repeated invocations allowed.";

        private const string OutputOption = "--output";
        private const string OutputHelp = "How PAT information is displayed. [default: token]\n[possible values: none, status, token, base64, header, headervalue, json]";

        private static readonly string LockfilePath = Path.Combine(Path.GetTempPath(), AzureAuth.Ado.Constants.PatLockfileName);

        // The possible PAT output modes.
        private enum OutputMode
        {
            // No output whatsoever.
            None,

            // Text indicating that a PAT was created/fetched and cached.
            Status,

            // Just the PAT, nothing more.
            Token,

            // A Base64-encoded version of the PAT.
            Base64,

            // The full `Authorization Basic` HTTP header.
            Header,

            // Just the value of the `Authorization Basic` header.
            HeaderValue,

            // The JSON value for the PAT, exactly as it was returned by the Azure DevOps API.
            Json,
        }

        [Option(OrganizationOption, OrganizationHelp, CommandOptionType.SingleValue)]
        private string Organization { get; set; } = null;

        [Option(DisplayNameOption, DisplayNameHelp, CommandOptionType.SingleValue)]
        private string DisplayName { get; set; } = null;

        [Option(ScopeOption, ScopeHelp, CommandOptionType.MultipleValue)]
        private IEnumerable<string> RawScopes { get; set; } = null;

        [Option(OutputOption, OutputHelp, CommandOptionType.SingleValue)]
        private OutputMode Output { get; set; } = OutputMode.Token;

        [Option(CommandAad.PromptHintOption, CommandAad.PromptHintHelpText, CommandOptionType.SingleValue)]
        private string PromptHint { get; set; } = null;

        [Option(CommandAad.TenantOption, Description = "The Azure Tenant ID to use for authentication. Defaults to Microsoft.")]
        private string Tenant { get; set; } = AzureAuth.Ado.Constants.Tenant.Microsoft;

        [Option(CommandAad.ModeOption, CommandAad.AuthModeHelperText, CommandOptionType.MultipleValue)]
        private IEnumerable<AuthMode> AuthModes { get; set; } = new[] { AuthMode.Default };

        [Option(CommandAad.DomainOption, $"{CommandAad.DomainHelpText}\n[default: {AzureAuth.Ado.Constants.PreferredDomain}]", CommandOptionType.SingleValue)]
        private string Domain { get; set; } = AzureAuth.Ado.Constants.PreferredDomain;

        [Option(CommandAad.TimeoutOption, CommandAad.TimeoutHelpText, CommandOptionType.SingleValue)]
        private double Timeout { get; set; } = CommandAad.GlobalTimeout.TotalMinutes;

        // Scopes are normalized from application start to prevent reparsing.
        private ImmutableSortedSet<string> _scopes;
        private ImmutableSortedSet<string> Scopes
        {
            get
            {
                if (this._scopes is null)
                {
                    this._scopes = AdoPat.Scopes.Normalize(this.RawScopes);
                }
                return this._scopes;
            }
        }

        /// <summary>
        /// Executes the command and returns a status code indicating the success or failure of the execution.
        /// </summary>
        /// <param name="logger">The <see cref="ILogger{T}"/> instance that is used for logging.</param>
        /// <param name="publicClientAuth">An <see cref="IPublicClientAuth"/>.</param>
        /// <param name="eventData">Lasso injected command event data.</param>
        /// <returns>An integer status code. 0 for success and non-zero for failure.</returns>
        public int OnExecute(ILogger<CommandPat> logger, IPublicClientAuth publicClientAuth, CommandExecuteEventData eventData)
        {
            if (!this.ValidOptions(logger))
            {
                return 1;
            }

            var accessToken = this.AccessToken(publicClientAuth, eventData);
            if (accessToken == null)
            {
                logger.LogError("Failed to acquire an Azure DevOps access token. Re-run with --verbosity=debug for more info.");
                return 1;
            }

            var cache = this.Cache();
            var client = this.Client(accessToken.Token);
            var manager = new PatManager(logger, cache, client);

            using (new CrossPlatLock(LockfilePath))
            {
                var pat = manager.GetPatAsync(this.PatOptions()).Result;

                // Do not use logger to avoid printing PATs into log files.
                Console.Write(FormatPat(pat, this.Output));
            }

            return 0;
        }

        private static string FormatPat(PatToken pat, OutputMode output) => output switch
        {
            OutputMode.None => string.Empty,
            OutputMode.Status => $"\"{pat.DisplayName}\" valid until {pat.ValidTo:O}\n",
            OutputMode.Token => pat.Token,
            OutputMode.Base64 => pat.Token.Base64(),
            OutputMode.Header => pat.Token.AsHeader(AzureAuth.Ado.Authorization.Basic),
            OutputMode.HeaderValue => pat.Token.AsHeaderValue(AzureAuth.Ado.Authorization.Basic),
            OutputMode.Json => pat.AsJson(),
            _ => throw new ArgumentOutOfRangeException(nameof(output)),
        };

        // This option validation could also be handled by using a [Required] attribute,
        // but for the best user experience we'd like to report multiple issues at once.
        private bool ValidOptions(ILogger logger)
        {
            bool validOptions = true;

            if (this.Scopes.IsEmpty)
            {
                logger.LogError($"The {ScopeOption} field is required.");
                validOptions = false;
            }
            else
            {
                var invalidScopes = AdoPat.Scopes.Validate(this.Scopes);
                if (!invalidScopes.IsEmpty)
                {
                    foreach (var scope in invalidScopes)
                    {
                        logger.LogError($"{scope} is not a valid Azure DevOps PAT scope.");
                    }
                    logger.LogError($"Consult {AdoPat.Constants.PatListURL} for a list of valid scopes.");
                    validOptions = false;
                }
            }

            if (string.IsNullOrEmpty(this.Organization))
            {
                logger.LogError($"The {OrganizationOption} field is required.");
                validOptions = false;
            }

            if (string.IsNullOrEmpty(this.DisplayName))
            {
                logger.LogError($"The {DisplayNameOption} field is required.");
                validOptions = false;
            }

            if (string.IsNullOrEmpty(this.PromptHint))
            {
                logger.LogError($"The {CommandAad.PromptHintOption} field is required.");
                validOptions = false;
            }

            return validOptions;
        }

        private TokenResult AccessToken(IPublicClientAuth publicClientAuth, CommandExecuteEventData eventData)
        {
            return publicClientAuth.Token(
                AzureAuth.Ado.AuthParameters.AdoParameters(this.Tenant),
                this.AuthModes,
                this.Domain,
                this.PromptHint,
                TimeSpan.FromMinutes(this.Timeout),
                eventData);
        }

        private PatOptions PatOptions()
        {
            return new PatOptions
            {
                Organization = this.Organization,
                DisplayName = this.DisplayName,
                Scopes = this.Scopes,
            };
        }

        private IPatClient Client(string accessToken)
        {
            var baseUrl = new Uri(string.Join('/', AzureAuth.Ado.Constants.BaseUrl, this.Organization));
            var credentials = new VssOAuthAccessTokenCredential(accessToken);
            var connection = new VssConnection(baseUrl, credentials);
            var tokensHttpClientWrapper = new TokensHttpClientWrapper(connection);
            return new PatClient(tokensHttpClientWrapper);
        }

        private IPatCache Cache()
        {
            var storageProperties = new StorageCreationPropertiesBuilder(
                PatStorageParameters.CacheFileName,
                AzureAuth.Constants.AppDirectory)
            .WithMacKeyChain(
                PatStorageParameters.MacOSServiceName,
                PatStorageParameters.MacOSAccountName)
            .WithLinuxKeyring(
                PatStorageParameters.LinuxKeyRingSchemaName,
                PatStorageParameters.LinuxKeyRingCollection,
                PatStorageParameters.LinuxKeyRingLabel,
                PatStorageParameters.LinuxKeyRingAttr1,
                PatStorageParameters.LinuxKeyRingAttr2)
            .Build();

            var storage = Storage.Create(storageProperties);
            var storageWrapper = new StorageWrapper(storage);
            return new PatCache(storageWrapper);
        }
    }
}
