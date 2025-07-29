# Linux Headless Cache Fallback Implementation Summary

## Overview

This implementation adds a plain text cache fallback for headless Linux environments where the Linux keyring is not available or fails to work properly.

## Files Modified/Created

### 1. `src/MSALWrapper/PCACache.cs` (Modified)
- Added Linux platform detection using `RuntimeInformation.IsOSPlatform(OSPlatform.Linux)`
- Added headless environment detection by checking `DISPLAY` and `WAYLAND_DISPLAY` environment variables
- Implemented `SetupPlainTextCache()` method for fallback cache configuration
- Added `SetDirectoryPermissions()` and `SetFilePermissions()` methods for secure file permissions
- Modified `SetupTokenCache()` to attempt plain text fallback when keyring fails on headless Linux

### 2. `src/MSALWrapper.Test/PCACacheTest.cs` (Created)
- Comprehensive test suite for the new functionality
- Tests platform detection logic
- Tests headless environment detection
- Tests cache file and directory creation
- Tests permission setting
- Tests error handling scenarios

### 3. `docs/linux-headless-cache-fallback.md` (Created)
- Complete documentation explaining the feature
- Usage instructions
- Security considerations
- Implementation details

### 4. `test-headless-cache.sh` (Created)
- Manual test script for Linux environments
- Simulates headless environment detection
- Tests cache directory and file creation
- Verifies permission settings

## Key Features Implemented

### 1. Automatic Detection
- Detects Linux platform using `RuntimeInformation.IsOSPlatform(OSPlatform.Linux)`
- Detects headless environment by checking for absence of display server variables:
  - `DISPLAY` environment variable
  - `WAYLAND_DISPLAY` environment variable

### 2. Secure Cache Storage
- Cache location: `~/.azureauth/msal_cache.json`
- Directory permissions: 700 (user only)
- File permissions: 600 (user only)
- Uses MSAL's `WithUnprotectedFile()` for plain text storage

### 3. Fallback Logic
- Only activates when keyring fails with `MsalCachePersistenceException`
- Only activates on Linux in headless environments
- Graceful error handling with detailed logging
- Maintains existing functionality for non-Linux platforms

### 4. Comprehensive Logging
- Information messages for fallback attempts
- Information messages for successful configuration
- Warning messages for permission setting failures
- Warning messages for fallback failures

## Security Considerations

1. **File Permissions**: Directory and file are created with restrictive permissions (700/600)
2. **User Isolation**: Only the current user can access the cache file
3. **Transparency**: Users are informed when plain text fallback is used
4. **Optional**: Can be disabled using existing `OEAUTH_MSAL_DISABLE_CACHE` environment variable

## Testing Strategy

### Unit Tests
- Platform detection tests
- Environment detection tests
- Error handling tests
- Cross-platform compatibility tests

### Manual Tests
- Linux headless environment simulation
- Permission verification
- Cache file creation and access

## Usage

The feature is completely transparent to users. When AzureAuth runs in a headless Linux environment and the keyring fails, it automatically falls back to the plain text cache without any user intervention required.

## Example Workflow

1. User runs AzureAuth in headless Linux environment (e.g., Docker container)
2. AzureAuth attempts to use Linux keyring for caching
3. Keyring fails with `MsalCachePersistenceException`
4. System detects Linux + headless environment
5. System creates `~/.azureauth/msal_cache.json` with proper permissions
6. System configures MSAL to use plain text cache
7. Subsequent runs use the cached tokens

## Benefits

1. **Improved User Experience**: No need to re-authenticate on every run in headless environments
2. **Backward Compatibility**: Existing functionality unchanged for non-Linux or non-headless environments
3. **Security**: Maintains security through proper file permissions
4. **Transparency**: Clear logging and documentation
5. **Reliability**: Graceful fallback with proper error handling

## Future Considerations

1. **Encryption**: Could add optional encryption for the plain text cache
2. **Configuration**: Could add environment variables to control fallback behavior
3. **Monitoring**: Could add telemetry for fallback usage
4. **Cleanup**: Could add cache cleanup utilities

## Compliance

- Follows existing code patterns and conventions
- Uses existing logging infrastructure
- Maintains backward compatibility
- Includes comprehensive documentation and tests 