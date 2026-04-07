#!/usr/bin/env bash
set -euo pipefail

CP="/Applications/Company Portal.app"
CP_OLD="/Applications/Company Portal (2602).app"
CP_NEW="/Applications/Company Portal (new).app"

get_version() {
    defaults read "$1/Contents/Info" CFBundleShortVersionString 2>/dev/null || echo "not found"
}

current=$(get_version "$CP")
echo "Current Company Portal: $current"
echo ""

if [ -d "$CP_OLD" ] && [ -d "$CP_NEW" ]; then
    echo "⚠️  Both backups exist — something's off. Check /Applications manually."
    ls -d /Applications/Company\ Portal*.app
    exit 1
fi

if [ -d "$CP_OLD" ]; then
    old_ver=$(get_version "$CP_OLD")
    echo "Backup available: $old_ver (old/2602)"
    echo ""
    echo "  [1] Switch to OLD ($old_ver)"
    echo "  [2] Do nothing"
    read -p "Choice: " choice
    if [ "$choice" = "1" ]; then
        mv "$CP" "$CP_NEW"
        mv "$CP_OLD" "$CP"
        echo "✅ Swapped to OLD — now: $(get_version "$CP")"
    fi
elif [ -d "$CP_NEW" ]; then
    new_ver=$(get_version "$CP_NEW")
    echo "Backup available: $new_ver (new/updated)"
    echo ""
    echo "  [1] Switch to NEW ($new_ver)"
    echo "  [2] Do nothing"
    read -p "Choice: " choice
    if [ "$choice" = "1" ]; then
        mv "$CP" "$CP_OLD"
        mv "$CP_NEW" "$CP"
        echo "✅ Swapped to NEW — now: $(get_version "$CP")"
    fi
else
    echo "No backup found. Nothing to swap."
fi
