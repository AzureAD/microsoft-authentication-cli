#!/usr/bin/env bash
set -euo pipefail

# Functional test script for macOS brokered auth changes
# Tests AzureAuth CLI with Work IQ's 3P Graph app registration
#
# Each interactive test has a timeout (default 30s). If azureauth hangs
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

# ── Test 2: Broker + web combined (explicit) ──────────────────
header "Test 2: Broker + web combined (--mode broker --mode web)"
if [ "$BROKER_AVAILABLE" = true ]; then
    echo "CP >= 2603 — broker will be tried first, web as fallback"
    EXPECTED_EXIT=0
else
    echo "CP < 2603 — broker requested but unavailable, expecting error"
    echo "(Error occurs before web is attempted because broker was explicitly requested)"
    EXPECTED_EXIT=1
fi
run_test "Broker + web combined" "$EXPECTED_EXIT" \
    aad --client "$CLIENT" --tenant "$TENANT" \
    --resource "$RESOURCE" \
    --mode broker --mode web --output json --verbosity "$VERBOSITY"

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

# ── Tests below may hang for broker-required apps (web flow) ──
header "⚠️  Remaining tests use web auth — may hang for broker-required apps"
echo "Ctrl+C or wait for timeout to skip individual tests."
echo ""

# ── Test 5: Default modes — web only (broker is opt-in on macOS) ──
header "Test 5: Default modes — web only on macOS"
echo "Default mode no longer includes broker. This tests web auth flow."
echo "If the app requires broker (token protection), web will hang — Ctrl+C."
run_test "Default modes (web only)" 0 \
    aad --client "$CLIENT" --tenant "$TENANT" \
    --resource "$RESOURCE" \
    --output json --verbosity "$VERBOSITY"

# ── Test 6: Web-only explicit (for apps that support it) ──────
header "Test 6: Web-only explicit (--mode web)"
echo "Explicit web flow. For broker-required apps, this will hang."
echo "For apps supporting web auth, this should open browser and succeed."
run_test "Web-only explicit" 0 \
    aad --client "$CLIENT" --tenant "$TENANT" \
    --resource "$RESOURCE" \
    --mode web --output json --verbosity "$VERBOSITY"

# ── Test 7: Explicit scopes (web) ─────────────────────────────
header "Test 7: Explicit Graph scopes (Mail.Read + Chat.Read, web)"
echo "Tests scope-based auth via web flow."
run_test "Explicit scopes (web)" 0 \
    aad --client "$CLIENT" --tenant "$TENANT" \
    --scope "https://graph.microsoft.com/Mail.Read" \
    --scope "https://graph.microsoft.com/Chat.Read" \
    --mode web --output token --verbosity "$VERBOSITY"

# ── Test 8: Silent re-auth (cached token) ─────────────────────
header "Test 7: Silent re-auth (should use cached token, no browser)"
echo "Running same command as Test 1 — should succeed silently from cache"
run_test "Silent re-auth (cached)" 0 \
    aad --client "$CLIENT" --tenant "$TENANT" \
    --resource "$RESOURCE" \
    --mode web --output json --verbosity "$VERBOSITY"

# ── Test 8: Clear cache ───────────────────────────────────────
header "Test 8: Clear token cache"
run_test "Cache clear" 0 \
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
