#!/usr/bin/env bash
set -euo pipefail

# Functional test script for macOS brokered auth changes
# Tests AzureAuth CLI with Work IQ's 3P Graph app registration
#
# Usage:
#   ./bin/mac/test-macos-broker.sh                              # defaults (debug verbosity, 120s timeout)
#   AZUREAUTH_TEST_VERBOSITY=info  ./bin/mac/test-macos-broker.sh   # less noise
#   AZUREAUTH_TEST_VERBOSITY=trace ./bin/mac/test-macos-broker.sh   # max detail
#   AZUREAUTH_TEST_TIMEOUT=60     ./bin/mac/test-macos-broker.sh   # shorter timeout
#
# Each interactive test has a timeout (default 120s). If azureauth hangs
# waiting for browser/broker, it will be killed and you can choose to
# mark it as SKIP or FAIL, then the script continues to the next test.
#
# You can also Ctrl+C during any individual test — the script traps it
# and moves on.

REPO_ROOT="$(cd "$(dirname "$0")/../.." && pwd)"
AZUREAUTH="$REPO_ROOT/src/AzureAuth/bin/Debug/net8.0/azureauth"
CLIENT="ba081686-5d24-4bc6-a0d6-d034ecffed87"
TENANT="common"
RESOURCE="https://graph.microsoft.com"
TIMEOUT="${AZUREAUTH_TEST_TIMEOUT:-120}"
VERBOSITY="${AZUREAUTH_TEST_VERBOSITY:-debug}"  # debug, trace, info, warn

PASS=0
FAIL=0
SKIP=0

header() {
    echo ""
    echo "========================================"
    echo "  $1"
    echo "========================================"
}

result() {
    local name="$1" exit_code="$2" expected="$3"
    if [ "$exit_code" -eq "$expected" ]; then
        echo "✅ PASS: $name (exit=$exit_code, expected=$expected)"
        PASS=$((PASS + 1))
    else
        echo "❌ FAIL: $name (exit=$exit_code, expected=$expected)"
        FAIL=$((FAIL + 1))
    fi
}

# Run azureauth with a timeout. On Ctrl+C or timeout, offer skip/fail.
# Usage: run_test "Test Name" expected_exit args...
run_test() {
    local test_name="$1" expected_exit="$2"
    shift 2

    echo ""
    echo "→ Running: azureauth $*"
    echo "  (timeout: ${TIMEOUT}s — Ctrl+C to abort this test)"
    echo ""

    local interrupted=false
    local pid=""

    # Trap SIGINT (Ctrl+C) for this test only
    trap 'interrupted=true; [ -n "$pid" ] && kill "$pid" 2>/dev/null' INT

    set +e
    # Run azureauth in background, then wait with timeout
    "$AZUREAUTH" "$@" 2>&1 &
    pid=$!

    # Wait up to TIMEOUT seconds for the process to finish
    local elapsed=0
    while [ "$elapsed" -lt "$TIMEOUT" ] && kill -0 "$pid" 2>/dev/null; do
        sleep 1
        ((elapsed++))
        if [ "$interrupted" = true ]; then
            break
        fi
    done

    # If still running after timeout, kill it
    if kill -0 "$pid" 2>/dev/null; then
        kill "$pid" 2>/dev/null
        wait "$pid" 2>/dev/null
        if [ "$interrupted" = false ]; then
            echo ""
            echo "⏱️  Test timed out after ${TIMEOUT}s"
        fi
        interrupted=true
        EXIT_CODE=124
    else
        wait "$pid"
        EXIT_CODE=$?
    fi
    pid=""
    set -e

    # Restore default SIGINT behavior
    trap - INT

    if [ "$interrupted" = true ]; then
        echo ""
        echo "Test was interrupted/timed out."
        read -p "Mark as [s]kip or [f]ail? (s/f, default=s): " choice </dev/tty || choice="s"
        choice="${choice:-s}"
        if [[ "$choice" =~ ^[fF] ]]; then
            echo "❌ FAIL: $test_name (interrupted, marked as fail)"
            FAIL=$((FAIL + 1))
        else
            echo "⏭️  SKIP: $test_name (interrupted)"
            SKIP=$((SKIP + 1))
        fi
        return
    fi

    result "$test_name" "$EXIT_CODE" "$expected_exit"
}

# ── Step 0: Build ──────────────────────────────────────────────
header "Step 0: Building AzureAuth"
dotnet build "$REPO_ROOT/AzureAuth.sln" \
    --no-restore -c Debug -v quiet 2>&1 | tail -3

if [ ! -x "$AZUREAUTH" ]; then
    echo "❌ Build failed — binary not found at $AZUREAUTH"
    exit 1
fi
echo "✅ Binary ready: $AZUREAUTH"
echo "   Version: $("$AZUREAUTH" --version)"

# ── Step 0.5: CP version info ─────────────────────────────────
header "Step 0.5: Company Portal status"
CP_PLIST="/Applications/Company Portal.app/Contents/Info.plist"
if [ -f "$CP_PLIST" ]; then
    CP_VERSION=$(defaults read "/Applications/Company Portal.app/Contents/Info" CFBundleShortVersionString 2>/dev/null || echo "unknown")
    echo "Company Portal version: $CP_VERSION"
    # Extract release number (middle segment of 5.RRRR.B)
    RELEASE=$(echo "$CP_VERSION" | awk -F. '{print $2}')
    if [ "$RELEASE" -ge 2603 ] 2>/dev/null; then
        echo "⚡ CP >= 2603 — broker tests WILL attempt real broker auth"
        BROKER_AVAILABLE=true
    else
        echo "⚠️  CP $CP_VERSION (release $RELEASE) < 2603 — broker will be gated off"
        BROKER_AVAILABLE=false
    fi
else
    echo "⚠️  Company Portal not installed — broker will be gated off"
    BROKER_AVAILABLE=false
fi

# ── Test 1: Broker-only mode (opt-in) ────────────────────────
header "Test 1: Broker-only mode (--mode broker)"
if [ "$BROKER_AVAILABLE" = true ]; then
    echo "CP >= 2603 detected — this will attempt real broker auth"
    echo "Expect: broker interactive prompt via Enterprise SSO Extension"
    EXPECTED_EXIT=0
else
    echo "CP < 2603 or not installed — expecting clear error about Company Portal"
    echo "Expect: InvalidOperationException with CP version/path info"
    EXPECTED_EXIT=1
fi
run_test "Broker-only (opt-in)" "$EXPECTED_EXIT" \
    aad --client "$CLIENT" --tenant "$TENANT" \
    --resource "$RESOURCE" \
    --mode broker --output json --verbosity "$VERBOSITY"

# ── Test 2: Broker + web combined — COMMENTED OUT ─────────────
# This app requires broker (token protection CA policy), so web auth
# will hang indefinitely waiting for a redirect that never comes.
# Uncomment for apps that support both broker and web auth.
#
# header "Test 2: Broker + web combined (--mode broker --mode web)"
# if [ "$BROKER_AVAILABLE" = true ]; then
#     echo "CP >= 2603 — broker will be tried first, web as fallback"
#     EXPECTED_EXIT=0
# else
#     echo "CP unavailable — broker skipped, falls through to web"
#     EXPECTED_EXIT=0
# fi
# run_test "Broker + web combined" "$EXPECTED_EXIT" \
#     aad --client "$CLIENT" --tenant "$TENANT" \
#     --resource "$RESOURCE" \
#     --mode broker --mode web --output json --verbosity "$VERBOSITY"

# ── Test 3: Trace verbosity — verify CP diagnostics in logs ───
header "Test 3: Trace verbosity — CP diagnostic logging"
echo "Running with --verbosity trace to verify Company Portal metadata is logged."
echo "🔍 Watch for: CP path, raw version output, release parsing"
if [ "$BROKER_AVAILABLE" = true ]; then
    EXPECTED_EXIT=0
else
    EXPECTED_EXIT=1
fi
run_test "Trace CP diagnostics" "$EXPECTED_EXIT" \
    aad --client "$CLIENT" --tenant "$TENANT" \
    --resource "$RESOURCE" \
    --mode broker --output json --verbosity trace

# ── Test 4: Clear cache ───────────────────────────────────────
header "Test 4: Clear token cache"
run_test "Cache clear" 0 \
    aad --client "$CLIENT" --tenant "$TENANT" \
    --resource "$RESOURCE" \
    --clear --verbosity "$VERBOSITY"

# ── Test 5: Broker + web fallthrough — COMMENTED OUT ──────────
# Same issue as Test 2: this app requires broker, so web will hang.
# Uncomment for apps that support both broker and web auth.
#
# header "Test 5: Broker + web fallthrough (--mode broker --mode web)"
# echo "Tests the fallthrough pattern: broker tried first, web as fallback."
# if [ "$BROKER_AVAILABLE" = true ]; then
#     echo "CP available — broker should succeed silently from Test 1 cache"
#     EXPECTED_EXIT=0
# else
#     echo "CP unavailable — broker skipped, falls through to web"
#     EXPECTED_EXIT=0
# fi
# run_test "Broker + web fallthrough" "$EXPECTED_EXIT" \
#     aad --client "$CLIENT" --tenant "$TENANT" \
#     --resource "$RESOURCE" \
#     --mode broker --mode web --output json --verbosity "$VERBOSITY"

# ── Test 6: Clear cache (before re-testing broker interactive) ─
header "Test 6: Clear token cache"
run_test "Cache clear (pre-broker retest)" 0 \
    aad --client "$CLIENT" --tenant "$TENANT" \
    --resource "$RESOURCE" \
    --clear --verbosity "$VERBOSITY"

# ── Test 7: Broker interactive again (after cache clear) ──────
header "Test 7: Broker interactive (after cache clear)"
if [ "$BROKER_AVAILABLE" = true ]; then
    echo "Cache was just cleared — broker must prompt interactively again"
    echo "Expect: broker account picker / SSO Extension prompt"
    EXPECTED_EXIT=0
else
    echo "CP unavailable — broker skipped, CachedAuth only (will fail)"
    EXPECTED_EXIT=1
fi
run_test "Broker interactive (re-prompt)" "$EXPECTED_EXIT" \
    aad --client "$CLIENT" --tenant "$TENANT" \
    --resource "$RESOURCE" \
    --mode broker --output json --verbosity "$VERBOSITY"

# ── Test 8: Final cache clear ─────────────────────────────────
header "Test 8: Final cache clear"
run_test "Cache clear (final)" 0 \
    aad --client "$CLIENT" --tenant "$TENANT" \
    --resource "$RESOURCE" \
    --clear --verbosity "$VERBOSITY"

# ── Summary ────────────────────────────────────────────────────
header "Results"
echo "✅ Passed: $PASS"
echo "⏭️  Skipped: $SKIP"
echo "❌ Failed: $FAIL"
echo ""
echo "Broker available: $BROKER_AVAILABLE"
if [ "$BROKER_AVAILABLE" = false ]; then
    echo "ℹ️  To test actual broker auth, upgrade Company Portal to >= 5.2603.x"
fi
echo ""
echo "Tip: Set AZUREAUTH_TEST_TIMEOUT=60 to change the per-test timeout (default: 30s)"
echo ""

if [ "$FAIL" -gt 0 ]; then
    exit 1
fi
