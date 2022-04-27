# Changelog
All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]
### Fixed
- Fixed a bug where device code flow authentication would not use the file cache to first attempt to get a cached token silently, causing it to always prompt.
- Fixed a bug where the Windows installation script could encounter errors renaming the extracted directory.

### Changed
- The installation scripts now extract to directories named after the release artifact from GitHub.
- The `latest` directory is now a [directory junction](https://docs.microsoft.com/en-us/windows/win32/fileio/hard-links-and-junctions#junctions) on Windows.
- The Option `--prompt-hint` will have a prefix `AzureAuth` by default.

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

[Unreleased]: https://github.com/AzureAD/microsoft-authentication-cli/compare/v0.2.0...HEAD
[v0.2.0]: https://github.com/AzureAD/microsoft-authentication-cli/compare/v0.1.0...v0.2.0
[v0.1.0]: https://github.com/AzureAD/microsoft-authentication-cli/releases/tag/v0.1.0
