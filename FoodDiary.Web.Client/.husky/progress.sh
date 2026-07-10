HOOK_HEARTBEAT_INTERVAL_SECONDS=${HOOK_HEARTBEAT_INTERVAL_SECONDS:-30}
HOOK_HEARTBEAT_PID=""
HOOK_NAME="hook"
HOOK_STEP=0
HOOK_TOTAL=0
HOOK_STARTED_AT=0
HOOK_STEP_STARTED_AT=0
HOOK_STEP_LABEL=""

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
  trap 'hook_stop_heartbeat' EXIT
  trap 'hook_stop_heartbeat; exit 130' INT
  trap 'hook_stop_heartbeat; exit 143' TERM
  printf '\n=== %s: %s checks ===\n' "$HOOK_NAME" "$HOOK_TOTAL"
}

hook_step_start() {
  HOOK_STEP=$((HOOK_STEP + 1))
  HOOK_STEP_LABEL=$1
  HOOK_STEP_STARTED_AT=$(date +%s)
  printf '\n>>> [%s %s/%s] %s\n' "$HOOK_NAME" "$HOOK_STEP" "$HOOK_TOTAL" "$HOOK_STEP_LABEL"

  (
    while sleep "$HOOK_HEARTBEAT_INTERVAL_SECONDS"; do
      hook_elapsed=$(($(date +%s) - HOOK_STEP_STARTED_AT))
      printf '... [%s %s/%s] still running: %s (%ss)\n' \
        "$HOOK_NAME" "$HOOK_STEP" "$HOOK_TOTAL" "$HOOK_STEP_LABEL" "$hook_elapsed"
    done
  ) &
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
  if "$@"; then
    hook_step_done
    return 0
  else
    hook_run_status=$?
    hook_step_fail
    return "$hook_run_status"
  fi
}

hook_progress_done() {
  hook_elapsed=$(($(date +%s) - HOOK_STARTED_AT))
  printf '\n=== %s completed: %s/%s checks in %ss ===\n' "$HOOK_NAME" "$HOOK_STEP" "$HOOK_TOTAL" "$hook_elapsed"
}
