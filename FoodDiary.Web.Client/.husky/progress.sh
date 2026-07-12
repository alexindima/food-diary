HOOK_HEARTBEAT_INTERVAL_SECONDS=${HOOK_HEARTBEAT_INTERVAL_SECONDS:-30}
HOOK_HEARTBEAT_PID=""
HOOK_NAME="hook"
HOOK_STEP=0
HOOK_TOTAL=0
HOOK_STARTED_AT=0
HOOK_STEP_STARTED_AT=0
HOOK_STEP_LABEL=""
HOOK_LOG_DIR=""
HOOK_FAILURE_LOG_LINES=${HOOK_FAILURE_LOG_LINES:-80}

hook_stop_heartbeat() {
  if [ -n "$HOOK_HEARTBEAT_PID" ]; then
    kill "$HOOK_HEARTBEAT_PID" 2>/dev/null || true
    wait "$HOOK_HEARTBEAT_PID" 2>/dev/null || true
    HOOK_HEARTBEAT_PID=""
  fi
}

hook_progress_init() {
  HOOK_NAME=$1
  HOOK_TOTAL=$2
  HOOK_STEP=0
  HOOK_STARTED_AT=$(date +%s)
  hook_git_dir=$(git rev-parse --absolute-git-dir 2>/dev/null || git rev-parse --git-dir 2>/dev/null || printf '.git')
  HOOK_LOG_DIR="$hook_git_dir/hook-logs/$HOOK_NAME-$HOOK_STARTED_AT"
  mkdir -p "$HOOK_LOG_DIR"
  trap 'hook_stop_heartbeat' EXIT
  trap 'hook_stop_heartbeat; exit 130' INT
  trap 'hook_stop_heartbeat; exit 143' TERM
  printf '\n=== %s: %s checks ===\n' "$HOOK_NAME" "$HOOK_TOTAL"
  printf 'Logs: %s\n' "$HOOK_LOG_DIR"
}

hook_step_start() {
  HOOK_STEP=$((HOOK_STEP + 1))
  HOOK_STEP_LABEL=$1
  HOOK_STEP_STARTED_AT=$(date +%s)
  printf '\n>>> [%s %s/%s] %s\n' "$HOOK_NAME" "$HOOK_STEP" "$HOOK_TOTAL" "$HOOK_STEP_LABEL"

  node -e '
    const [name, step, total, label, interval] = process.argv.slice(1);
    const startedAt = Date.now();
    setInterval(() => {
      const elapsed = Math.floor((Date.now() - startedAt) / 1000);
      console.log(`... [${name} ${step}/${total}] still running: ${label} (${elapsed}s)`);
    }, Number(interval) * 1000);
  ' "$HOOK_NAME" "$HOOK_STEP" "$HOOK_TOTAL" "$HOOK_STEP_LABEL" "$HOOK_HEARTBEAT_INTERVAL_SECONDS" &
  HOOK_HEARTBEAT_PID=$!
}

hook_step_done() {
  hook_stop_heartbeat
  hook_elapsed=$(($(date +%s) - HOOK_STEP_STARTED_AT))
  printf '<<< [%s %s/%s] completed in %ss: %s\n' \
    "$HOOK_NAME" "$HOOK_STEP" "$HOOK_TOTAL" "$hook_elapsed" "$HOOK_STEP_LABEL"
}

hook_step_fail() {
  hook_stop_heartbeat
  hook_elapsed=$(($(date +%s) - HOOK_STEP_STARTED_AT))
  printf 'xxx [%s %s/%s] failed after %ss: %s\n' \
    "$HOOK_NAME" "$HOOK_STEP" "$HOOK_TOTAL" "$hook_elapsed" "$HOOK_STEP_LABEL" >&2
}

hook_step_skip() {
  HOOK_STEP=$((HOOK_STEP + 1))
  printf -- '--- [%s %s/%s] skipped: %s (%s)\n' "$HOOK_NAME" "$HOOK_STEP" "$HOOK_TOTAL" "$1" "$2"
}

hook_run() {
  hook_run_label=$1
  shift
  hook_step_start "$hook_run_label"
  hook_log_slug=$(printf '%s' "$hook_run_label" | tr '[:upper:] ' '[:lower:]-' | tr -cd '[:alnum:]_-')
  hook_log_file="$HOOK_LOG_DIR/$(printf '%02d' "$HOOK_STEP")-$hook_log_slug.log"
  if "$@" >"$hook_log_file" 2>&1; then
    hook_step_done
    printf '    log: %s\n' "$hook_log_file"
    return 0
  else
    hook_run_status=$?
    hook_step_fail
    printf '    command:' >&2
    printf ' %s' "$@" >&2
    printf '\n    exit code: %s\n    log: %s\n' "$hook_run_status" "$hook_log_file" >&2
    printf '%s\n' "--- last $HOOK_FAILURE_LOG_LINES log lines ---" >&2
    tail -n "$HOOK_FAILURE_LOG_LINES" "$hook_log_file" >&2 || true
    printf '%s\n' '--- end log ---' >&2
    return "$hook_run_status"
  fi
}

hook_progress_done() {
  hook_elapsed=$(($(date +%s) - HOOK_STARTED_AT))
  printf '\n=== %s completed: %s/%s checks in %ss ===\n' "$HOOK_NAME" "$HOOK_STEP" "$HOOK_TOTAL" "$hook_elapsed"
}
