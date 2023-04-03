// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Authentication.AzureAuth.Commands.Ado
{
    using System;
    using System.Collections.Generic;
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

        private const string DomainOption = "--domain";
        private const string DomainHelp = "The preferred domain used when acquiring Azure Active Directory access tokens. [default: microsoft.com]";

        private const string AccessTokenPrompt = "AzureAuth ADO PAT";
        private static readonly IEnumerable<AuthMode> AccessTokenAuthModes = new[] { AuthMode.Default };
        private static readonly TimeSpan AccessTokenTimeout = TimeSpan.FromMinutes(15);

        private static readonly string LockfilePath = Path.Combine(Path.GetTempPath(), AzureAuth.Ado.Constants.PatLockfileName);

        // On all platforms the PAT cache should be in the same directory as a typical AzureAuth installation.
        //   - On Windows this is usually `%LOCALAPPDATA%\Programs\AzureAuth`.
        //   - On Unix-like platforms this is usually `~/.azureauth`.
#if PlatformWindows
        private static readonly string CacheDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Programs",
            "AzureAuth");
#else
        private static readonly string CacheDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".azureauth");
#endif

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
        private string[] Scopes { get; set; } = null;

        [Option(OutputOption, OutputHelp, CommandOptionType.SingleValue)]
        private OutputMode Output { get; set; } = OutputMode.Token;

        [Option(DomainOption, DomainHelp, CommandOptionType.SingleValue)]
        private string Domain { get; set; } = "microsoft.com";

        /// <summary>
        /// Executes the command and returns a status code indicating the success or failure of the execution.
        /// </summary>
        /// <param name="logger">The <see cref="ILogger{T}"/> instance that is used for logging.</param>
        /// <param name="publicClientAuth">An <see cref="IPublicClientAuth"/>.</param>
        /// <param name="eventData">Lasso injected command event data.</param>
        /// <returns>An integer status code. 0 for success and non-zero for failure.</returns>
        public int OnExecute(ILogger<CommandPat> logger, IPublicClientAuth publicClientAuth, CommandExecuteEventData eventData)
        {
            var accessToken = this.AccessToken(publicClientAuth, eventData);
            if (accessToken == null)
            {
                logger.LogError("Failed to acquire an Azure DevOps access token. Re-run with '--verbosity debug' for more info.");
                return 1;
            }

            var patOptions = new PatOptions
            {
                Organization = this.Organization,
                DisplayName = this.DisplayName,
                Scopes = this.Scopes,
            };

            var cache = this.Cache();
            var client = this.Client(accessToken.Token);
            var manager = new PatManager(cache, client);

            using (new CrossPlatLock(LockfilePath))
            {
                var pat = manager.GetPatAsync(patOptions).Result;

                // Do not use logger to avoid printing PATs into log files.
                Console.WriteLine(FormatPat(pat, this.Output));
            }

            return 0;
        }

        private static string FormatPat(PatToken pat, OutputMode output) => output switch
        {
            OutputMode.None => string.Empty,
            OutputMode.Status => $"\"{pat.DisplayName}\" valid until {pat.ValidTo:O}",
            OutputMode.Token => pat.Token,
            OutputMode.Base64 => pat.Token.Base64(),
            OutputMode.Header => pat.Token.AsHeader(AzureAuth.Ado.Authorization.Basic),
            OutputMode.HeaderValue => pat.Token.AsHeaderValue(AzureAuth.Ado.Authorization.Basic),
            OutputMode.Json => pat.AsJson(),
            _ => throw new ArgumentOutOfRangeException(nameof(output)),
        };

        private TokenResult AccessToken(IPublicClientAuth publicClientAuth, CommandExecuteEventData eventData)
        {
            return publicClientAuth.Token(
                AzureAuth.Ado.Constants.AdoParams,
                AccessTokenAuthModes,
                this.Domain,
                AccessTokenPrompt,
                AccessTokenTimeout,
                eventData);
        }

        private IPatClient Client(string accessToken)
        {
            var baseUrl = new Uri($"{AzureAuth.Ado.Constants.BaseUrl}/{this.Organization}");
            var credentials = new VssOAuthAccessTokenCredential(accessToken);
            var connection = new VssConnection(baseUrl, credentials);
            var tokensHttpClientWrapper = new TokensHttpClientWrapper(connection);
            return new PatClient(tokensHttpClientWrapper);
        }

        private IPatCache Cache()
        {
            var storageProperties = new StorageCreationPropertiesBuilder(
                PatStorageParameters.CacheFileName,
                CacheDirectory)
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

            // TODO: We probably need to do a `storage.VerifyPersistence` check
            // before using this. On Linux this won't work in a headless
            // environment, so we'll need to find a fallback or fail early.
            var storage = Storage.Create(storageProperties);
            var storageWrapper = new StorageWrapper(storage);
            return new PatCache(storageWrapper);
        }
    }
}
