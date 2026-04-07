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

REPO_ROOT="$(cd "$(dirname "$0")" && pwd)"
AZUREAUTH="$REPO_ROOT/src/AzureAuth/bin/Debug/net8.0/azureauth"
CLIENT="ba081686-5d24-4bc6-a0d6-d034ecffed87"
TENANT="common"
RESOURCE="https://graph.microsoft.com"
TIMEOUT="${AZUREAUTH_TEST_TIMEOUT:-30}"

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
        ((PASS++))
    else
        echo "❌ FAIL: $name (exit=$exit_code, expected=$expected)"
        ((FAIL++))
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
            ((FAIL++))
        else
            echo "⏭️  SKIP: $test_name (interrupted)"
            ((SKIP++))
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

# ── Test 1: Web flow (baseline) ───────────────────────────────
header "Test 1: Web flow — baseline auth (interactive, opens browser)"
echo "This will try to open a browser for sign-in."
echo "If the app requires broker, this will hang — Ctrl+C or wait for timeout."
run_test "Web flow baseline" 0 \
    aad --client "$CLIENT" --tenant "$TENANT" \
    --resource "$RESOURCE" \
    --mode web --output json --verbosity debug

# ── Test 2: Default modes (broker,web) ────────────────────────
header "Test 2: Default modes — broker + web (broker skipped if CP < 2603)"
echo "🔍 Watch for: 'Company Portal version' or 'broker' log lines"
run_test "Default modes (broker+web)" 0 \
    aad --client "$CLIENT" --tenant "$TENANT" \
    --resource "$RESOURCE" \
    --output json --verbosity debug

# ── Test 3: Broker-only mode ──────────────────────────────────
header "Test 3: Broker-only mode"
if [ "$BROKER_AVAILABLE" = true ]; then
    echo "CP >= 2603 detected — this will attempt real broker auth"
    EXPECTED_EXIT=0
else
    echo "CP < 2603 — broker gated off. Expecting failure (no available auth flows)"
    EXPECTED_EXIT=1
fi
run_test "Broker-only mode" "$EXPECTED_EXIT" \
    aad --client "$CLIENT" --tenant "$TENANT" \
    --resource "$RESOURCE" \
    --mode broker --output json --verbosity debug

# ── Test 4: Explicit scopes ───────────────────────────────────
header "Test 4: Explicit Graph scopes (Mail.Read + Chat.Read)"
echo "Opens browser for consent to specific scopes."
run_test "Explicit scopes" 0 \
    aad --client "$CLIENT" --tenant "$TENANT" \
    --scope "https://graph.microsoft.com/Mail.Read" \
    --scope "https://graph.microsoft.com/Chat.Read" \
    --mode web --output token --verbosity debug

# ── Test 5: Silent re-auth (cached token) ─────────────────────
header "Test 5: Silent re-auth (should use cached token, no browser)"
echo "Running same command as Test 1 — should succeed silently from cache"
run_test "Silent re-auth (cached)" 0 \
    aad --client "$CLIENT" --tenant "$TENANT" \
    --resource "$RESOURCE" \
    --mode web --output json --verbosity debug

# ── Test 6: Clear cache ───────────────────────────────────────
header "Test 6: Clear token cache"
run_test "Cache clear" 0 \
    aad --client "$CLIENT" --tenant "$TENANT" \
    --resource "$RESOURCE" \
    --clear --verbosity debug

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
