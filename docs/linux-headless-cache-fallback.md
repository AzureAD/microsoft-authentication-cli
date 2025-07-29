# Linux Headless Environment Cache Fallback

## Overview

AzureAuth now supports a plain text cache fallback for headless Linux environments where the Linux keyring is not available or fails to work properly.

## Problem

In headless Linux environments (such as CI/CD pipelines, Docker containers, or servers without a display server), the Linux keyring service may not be available or may fail to initialize properly. This prevents AzureAuth from caching authentication tokens, requiring users to re-authenticate on every run.

## Solution

When AzureAuth detects that it's running on Linux and the keyring-based cache fails to initialize, it automatically falls back to a plain text cache file stored in the user's home directory.

## Implementation Details

### Detection Logic

The system detects headless Linux environments by checking for the absence of display server environment variables:

- `DISPLAY` environment variable is not set or empty
- `WAYLAND_DISPLAY` environment variable is not set or empty

### Cache Location

The plain text cache is stored at:
```
~/.azureauth/msal_cache.json
```

### Security

The cache directory and file are created with restrictive permissions:

- Directory (`~/.azureauth`): 700 (user read/write/execute, no permissions for group/others)
- File (`msal_cache.json`): 600 (user read/write, no permissions for group/others)

This ensures that only the current user can access the cache file.

### Fallback Process

1. AzureAuth attempts to use the Linux keyring for token caching
2. If the keyring fails with a `MsalCachePersistenceException`
3. The system checks if it's running on Linux and in a headless environment
4. If both conditions are met, it creates the `~/.azureauth` directory with proper permissions
5. It creates the `msal_cache.json` file with proper permissions
6. It configures MSAL to use the plain text cache file instead of the keyring

### Logging

The implementation provides detailed logging:

- Information message when plain text fallback is attempted
- Information message when plain text cache is successfully configured
- Warning messages if directory or file permission setting fails
- Warning messages if the plain text fallback itself fails

## Usage

No configuration is required. The fallback is automatic and transparent to users. When running in a headless Linux environment where the keyring fails, AzureAuth will automatically use the plain text cache.

## Example

```bash
# In a headless Linux environment (e.g., Docker container)
$ azureauth aad token --client-id <client-id> --tenant-id <tenant-id> --scope <scope>
# First run: User will be prompted for authentication
# Subsequent runs: Token will be retrieved from ~/.azureauth/msal_cache.json
```

## Security Considerations

- The plain text cache is stored unencrypted on disk
- Access is restricted to the current user only through file system permissions
- Users should be aware that tokens are stored in plain text
- The cache file should be included in `.gitignore` if the home directory is version controlled

## Disabling the Fallback

To disable the plain text cache fallback, set the environment variable:
```bash
export OEAUTH_MSAL_DISABLE_CACHE=1
```

This will disable all caching, including both the keyring and plain text fallback.

## Testing

The implementation includes comprehensive tests that verify:
- Platform detection logic
- Headless environment detection
- Cache file and directory creation
- Permission setting
- Error handling

Tests are designed to work on both Linux and non-Linux platforms, with platform-specific tests being skipped when not applicable. 