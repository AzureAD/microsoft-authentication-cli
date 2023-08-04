# Changelog
All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]
### Added
- Added support for distributing debian packages.

### Fixed
- AzureAuth now can handle SIGINT(Ctrl+C) correctly and return 2.

## [0.8.2] - 2023-07-06
### Added
- The `azureauth ado` subcommands now support a `--tenant` flag.

### Changed
- The `azureauth ado pat` subcommand now validates `--scope` before creating a PAT.

## [0.8.1] - 2023-05-23
### Changed
- The `azureauth ado token` command uses `microsoft.com` as the default `--domain` option value.
- MSAL Cache usage is now isolated to it's own "auth flow" always injected as the first type of auth to attempt, regardless of mode. This creates a separate telemetry event for `pca_cache` as a new authflow type, which is always silent. The remaining authflows no longer attempt to use the cache first.
- Upgraded Lasso to 2023.5.11.1 to reduce the number of log files in temp folder.

### Fixed
- In several auth flows, it was possible that errors in using the cache could result in never attempting to do interactive auth when the tool should have. 

## [0.8.0] - 2023-04-07
### Added
- New `ado` sub-commands
  - `azureauth ado` : Prints the help for Azure Devops commands.
  - `azureauth ado pat` : Command for creating, and locally caching Azure Devops <abbr title="Personal Access Tokens">PAT</abbr>s.
  - `azureauth ado token` : Command for passing back a <abbr title="Personal Access Tokens">PAT</abbr> from an env var, or authenticating and returning an <abbr title="Azure Active Directory">AAD</abbr> access token.

### Removed
- The root command `azureauth` no longer acquires AAD tokens. It prints the global help text. Use `azureauth aad` instead.

## [0.7.4] - 2023-04-05
### Added
- When the environment variable `AZUREAUTH_APPLICATION_INSIGHTS_INGESTION_TOKEN` is not configured,
 regkey `HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\AzureAuth\ApplicationInsightsIngestionToken` will be a back up on Windows for telemetry ingestion token config.

## [0.7.3] - 2023-03-14
### Fixed
- Fix unusable issues on Unix platforms due to mutexes.

## [0.7.2] - 2023-03-08
### Fixed
- Upgrade to a new version of Lasso which patches a concurrency bug.

## [0.7.1] - 2023-03-03
### Fixed
- Upgrade to a new version of Lasso which patches a concurrency bug.

## [0.7.0] - 2023-02-22
### Added
- New telemetry fields for:
  - environment variables identifying Azure Pipelines and Cloud Build environments.
  - on-premises security identifier as `sid`.
    This is only collected on successful authentication attempts.
- New `aad` command. This command is the long-term home for what is currently the top-level `azureauth` command. The functionality is duplicated in both commands for backwards compatibility but will be removed from the top-level command in a future release.
- New `info` commands
  - `azureauth info` : reports AzureAuth version and a new local randomly generated and cached telemetry device ID.
  - `azureauth info reset-device-id` : regenerates the cached telemetry device id.

### Changed
- Migrate from single command <abbr title="Command Line Interface">CLI</abbr> to sub-command structure.
  - Existing `azureauth` command is now replicated as `azureauth aad`.
- Upgrade MSAL to 4.47.2 and opt-into native WAM mode.
- Improve error telemetry collection by collecting JSON serialized version of MSAL errors. This now includes inner exceptions from MSAL which previously were missed.

### Fixed
- Replace `setx` usage with `WM_SETTINGCHANGE` in the Windows install script to prevent truncating `$PATH`.
- Skip validating cache file logic on Mac, which could cause an unhandled exception when certain Special Folders don't exist for the current user.
- Updated Lasso, fixing an issue where callers shelling out to AzureAuth were blocked on the asynchronous telemetry child processes.

### Removed
- Removed the `--cache` option from what is now `azureauth aad`, because cache file sharing is not the recommended way to achieve <abbr title="Single Sign On">SSO</abbr>.

## [0.6.0] - 2022-10-26
### Fixed
- Use system web browser as the UI for web mode auth on Windows to prevent conditional access based over-prompting.
- Catch `FileNotFoundException` if an invalid configuration file is specified via the `AZUREAUTH_CONFIG` environment variable.

### Changed
- Upgrade the Windows build to use net6 now that net5 has reached end of life.
- Set console output encoding to `utf-8` explicitly.

## [0.5.4] - 2022-09-29
### Fixed
- Enable IWA authmode when interactive authentication is disabled.

## [0.5.3] - 2022-09-28
### Fixed
- Increase IWA Timeout to 15 second and log WS-Trust endpoint error

## [0.5.2] - 2022-09-28
### Fixed
- Option `--resource` is not needed if option `--scope` is provided.
- Refactoring IWA AuthFlow to call GetTokenIWA when we have a MsalUiRequiredException

## [0.5.1] - 2022-09-08
### Fixed
- Fixed a bug where we early exited before sending individual events telemetry data containing valuable `error_messages`.

## [0.5.0] - 2022-09-06
### Added
- Added functionality to disable Public Client Authentication using an environment variable `AZUREAUTH_NO_USER`.
- Added `--timeout` functionality to provide reliable contract of allowed runtime (default: 15 minutes) and warnings as the timeout approaches.

- Fixed a bug where broker auth prompt is hanging in the background and gives a false impression to the user that the console app is hung.
- Fixed a bug where sometimes, when logged in with only a password (not a strong form of authentication) the broker flow could hang indefinitely, preventing fall back to another auth flow.

## [0.4.0] - 2022-06-23
### Added
- Environment variable `AZUREAUTH_CACHE` and option `--cache` to support a custom cache location on Windows.
- Added Integrated Windows Authentication (IWA) functionality as the new default auth flow on Windows.
- Send custom telemetry events for each AuthFlow.
- The installation scripts will refuse to update the user's `$PATH` or shell profiles when given the `-NoUpdatePath`
  flag (on Windows) or if the `$AZUREAUTH_NO_UPDATE_PATH` environment variable is set (on Unix platforms).

### Changed
- The installation scripts no longer create a `latest` symlink/junction.

## [0.3.1] - 2022-06-07
### Fixed
- Fixed a bug where the tenant and resource ids were swapped in the telemetry events.

### Changed
- The version schema no longer has a `v` prefix (e.g. `v0.3.1` is now expressed as `0.3.1`).

## [v0.3.0] - 2022-05-03
### Fixed
- Fixed a bug to support running on Windows Server 2012 & 2016 by default (default auth mode for Windows is now broker + web).
- Fixed a bug where device code flow authentication would not use the file cache to first attempt to get a cached token silently, causing it to always prompt.
- Fixed a bug where the Windows installation script could encounter errors renaming the extracted directory.

### Changed
- Telemetry: If enabled, collect the app registration ids being used and whether args were valid.
- The default for `--mode` on Windows is now `broker` + `web` (formerly just `broker`).
- The installation scripts now extract to directories named after the release artifact from GitHub.
- The `latest` directory is now a [directory junction](https://docs.microsoft.com/en-us/windows/win32/fileio/hard-links-and-junctions#junctions) on Windows.
- The Option `--prompt-hint` will have a prefix `AzureAuth`.

### Removed
- Removed sample projects that used the old `TokenFetcherPublicClient` api from the MSALWrapper project.

## [v0.2.0] - 2022-04-21
### Security
- Fix a bug that caused tokens to be written to log files.

### Added
- Option `--prompt-hint` to support custom text to prompt caller in web and WAM mode.

### Changed
- Rename the `--auth-mode` flag to `--mode`.
- Update to MSAL 4.43.1.

### Removed
- The `-t`, `-c`, `-d`, `-m`, and `-o` short flags.

## [v0.1.0] - 2022-03-30
### Added
- Initial project release.

[Unreleased]: https://github.com/AzureAD/microsoft-authentication-cli/compare/0.8.2...HEAD
[0.8.2]: https://github.com/AzureAD/microsoft-authentication-cli/compare/0.8.1...0.8.2
[0.8.1]: https://github.com/AzureAD/microsoft-authentication-cli/compare/0.8.0...0.8.1
[0.8.0]: https://github.com/AzureAD/microsoft-authentication-cli/compare/0.7.4...0.8.0
[0.7.4]: https://github.com/AzureAD/microsoft-authentication-cli/compare/0.7.3...0.7.4
[0.7.3]: https://github.com/AzureAD/microsoft-authentication-cli/compare/0.7.2...0.7.3
[0.7.2]: https://github.com/AzureAD/microsoft-authentication-cli/compare/0.7.1...0.7.2
[0.7.1]: https://github.com/AzureAD/microsoft-authentication-cli/compare/0.7.0...0.7.1
[0.7.0]: https://github.com/AzureAD/microsoft-authentication-cli/compare/0.6.0...0.7.0
[0.6.0]: https://github.com/AzureAD/microsoft-authentication-cli/compare/0.5.4...0.6.0
[0.5.4]: https://github.com/AzureAD/microsoft-authentication-cli/compare/0.5.3...0.5.4
[0.5.3]: https://github.com/AzureAD/microsoft-authentication-cli/compare/0.5.2...0.5.3
[0.5.2]: https://github.com/AzureAD/microsoft-authentication-cli/compare/0.5.1...0.5.2
[0.5.1]: https://github.com/AzureAD/microsoft-authentication-cli/compare/0.5.0...0.5.1
[0.5.0]: https://github.com/AzureAD/microsoft-authentication-cli/compare/0.4.0...0.5.0
[0.4.0]: https://github.com/AzureAD/microsoft-authentication-cli/compare/0.3.1...0.4.0
[0.3.1]: https://github.com/AzureAD/microsoft-authentication-cli/compare/v0.3.0...0.3.1
[v0.3.0]: https://github.com/AzureAD/microsoft-authentication-cli/compare/v0.2.0...v0.3.0
[v0.2.0]: https://github.com/AzureAD/microsoft-authentication-cli/compare/v0.1.0...v0.2.0
[v0.1.0]: https://github.com/AzureAD/microsoft-authentication-cli/releases/tag/v0.1.0
