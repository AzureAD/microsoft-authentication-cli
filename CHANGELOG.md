# Changelog
All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]
### Added
- Implemented Integrated Windows Authentication as one of the auth flows.

## [Unreleased]
### Added
- Environment variable `AZUREAUTH_CACHE_FILE` and option `--cache` to support a custom cache location on Windows.

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

[Unreleased]: https://github.com/AzureAD/microsoft-authentication-cli/compare/0.3.1...HEAD
[0.3.1]: https://github.com/AzureAD/microsoft-authentication-cli/compare/v0.3.0...0.3.1
[v0.3.0]: https://github.com/AzureAD/microsoft-authentication-cli/compare/v0.2.0...v0.3.0
[v0.2.0]: https://github.com/AzureAD/microsoft-authentication-cli/compare/v0.1.0...v0.2.0
[v0.1.0]: https://github.com/AzureAD/microsoft-authentication-cli/releases/tag/v0.1.0
