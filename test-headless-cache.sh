#!/bin/bash

# Test script for Linux headless cache fallback
# This script simulates a headless Linux environment and tests the cache fallback

set -e

echo "Testing Linux headless cache fallback..."

# Check if we're on Linux
if [[ "$OSTYPE" != "linux-gnu"* ]]; then
    echo "This test is designed for Linux environments only."
    echo "Current OS: $OSTYPE"
    exit 0
fi

# Function to check if environment is headless
is_headless() {
    if [[ -z "$DISPLAY" && -z "$WAYLAND_DISPLAY" ]]; then
        return 0  # true - headless
    else
        return 1  # false - not headless
    fi
}

# Function to check if .azureauth directory exists
check_cache_dir() {
    local home_dir="$HOME"
    local cache_dir="$home_dir/.azureauth"
    local cache_file="$cache_dir/msal_cache.json"
    
    if [[ -d "$cache_dir" ]]; then
        echo "✓ Cache directory exists: $cache_dir"
        
        # Check directory permissions
        local dir_perms=$(stat -c "%a" "$cache_dir")
        if [[ "$dir_perms" == "700" ]]; then
            echo "✓ Directory permissions are correct: $dir_perms"
        else
            echo "✗ Directory permissions are incorrect: $dir_perms (expected: 700)"
        fi
        
        if [[ -f "$cache_file" ]]; then
            echo "✓ Cache file exists: $cache_file"
            
            # Check file permissions
            local file_perms=$(stat -c "%a" "$cache_file")
            if [[ "$file_perms" == "600" ]]; then
                echo "✓ File permissions are correct: $file_perms"
            else
                echo "✗ File permissions are incorrect: $file_perms (expected: 600)"
            fi
        else
            echo "✗ Cache file does not exist: $cache_file"
        fi
    else
        echo "✗ Cache directory does not exist: $cache_dir"
    fi
}

# Function to simulate headless environment
simulate_headless() {
    echo "Simulating headless environment..."
    
    # Save original environment variables
    local original_display="$DISPLAY"
    local original_wayland_display="$WAYLAND_DISPLAY"
    
    # Unset display variables to simulate headless environment
    unset DISPLAY
    unset WAYLAND_DISPLAY
    
    echo "Environment variables:"
    echo "  DISPLAY: ${DISPLAY:-'not set'}"
    echo "  WAYLAND_DISPLAY: ${WAYLAND_DISPLAY:-'not set'}"
    
    if is_headless; then
        echo "✓ Environment is correctly detected as headless"
    else
        echo "✗ Environment is not detected as headless"
    fi
    
    # Restore original environment variables
    export DISPLAY="$original_display"
    export WAYLAND_DISPLAY="$original_wayland_display"
}

# Function to test cache directory creation
test_cache_creation() {
    echo "Testing cache directory creation..."
    
    local home_dir="$HOME"
    local cache_dir="$home_dir/.azureauth"
    local cache_file="$cache_dir/msal_cache.json"
    
    # Remove existing cache directory if it exists
    if [[ -d "$cache_dir" ]]; then
        echo "Removing existing cache directory..."
        rm -rf "$cache_dir"
    fi
    
    # Create cache directory
    echo "Creating cache directory..."
    mkdir -p "$cache_dir"
    
    # Set directory permissions
    echo "Setting directory permissions..."
    chmod 700 "$cache_dir"
    
    # Create cache file
    echo "Creating cache file..."
    echo "{}" > "$cache_file"
    
    # Set file permissions
    echo "Setting file permissions..."
    chmod 600 "$cache_file"
    
    # Verify creation
    check_cache_dir
    
    # Clean up
    echo "Cleaning up test cache directory..."
    rm -rf "$cache_dir"
}

# Main test execution
echo "=== Linux Headless Cache Fallback Test ==="
echo ""

echo "1. Checking current environment..."
if is_headless; then
    echo "✓ Current environment is headless"
else
    echo "ℹ Current environment is not headless (has display server)"
fi
echo ""

echo "2. Checking for existing cache directory..."
check_cache_dir
echo ""

echo "3. Simulating headless environment..."
simulate_headless
echo ""

echo "4. Testing cache directory creation..."
test_cache_creation
echo ""

echo "=== Test Complete ==="
echo ""
echo "Note: This script tests the infrastructure for the cache fallback."
echo "To test the actual AzureAuth integration, you would need to:"
echo "1. Build the AzureAuth project"
echo "2. Run AzureAuth in a headless Linux environment"
echo "3. Verify that tokens are cached in ~/.azureauth/msal_cache.json" 