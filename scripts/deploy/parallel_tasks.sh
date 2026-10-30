#!/usr/bin/env bash
# Run exported deployment functions with bounded workers and complete exit accounting.
set -euo pipefail
umask 077

if [[ $# -lt 3 || ! -d "$1" || -L "$1" || $(stat -c '%a' "$1") != 700 ]]; then
    echo 'A private existing log directory, concurrency and task names are required.' >&2
    exit 64
fi
log_directory=$1
limit=$2
shift 2
if [[ "$limit" != 1 && "$limit" != 2 ]]; then
    echo 'Deployment task concurrency must be one or two.' >&2
    exit 64
fi

declare -A seen=()
for task in "$@"; do
    if [[ ! "$task" =~ ^[a-z][a-z0-9_]{0,63}$ ]] ||
        [[ -n ${seen[$task]:-} || -e "$log_directory/$task.log" || -L "$log_directory/$task.log" ]] ||
        ! declare -F "$task" >/dev/null; then
        echo 'Invalid, duplicate or unavailable deployment task.' >&2
        exit 64
    fi
    seen[$task]=1
done

pids=()
names=()
failed=0

reap_first() {
    local status=0
    local name=${names[0]}
    # Waiting by PID also retains errors from workers that already exited.
    wait "${pids[0]}" || status=$?
    if ! printf 'Deployment task %s finished: exit=%s\n' "$name" "$status"; then
        failed=1
    fi
    if ! cat -- "$log_directory/$name.log"; then
        failed=1
    fi
    if [[ "$status" != 0 ]]; then
        failed=1
    fi
    pids=("${pids[@]:1}")
    names=("${names[@]:1}")
    return 0
}

finish() {
    local status=$?
    trap - EXIT
    while [[ ${#pids[@]} -gt 0 ]]; do
        reap_first
    done
    if [[ "$status" == 0 && "$failed" != 0 ]]; then
        status=1
    fi
    exit "$status"
}
trap finish EXIT
trap 'exit 143' TERM
trap 'exit 130' INT
trap 'exit 129' HUP

for task in "$@"; do
    while [[ ${#pids[@]} -ge $limit ]]; do
        reap_first
    done
    # A fresh shell keeps errexit effective inside multi-command task functions.
    bash -e -o pipefail -c "$task" > "$log_directory/$task.log" 2>&1 &
    pids+=("$!")
    names+=("$task")
    printf 'Deployment task %s started\n' "$task"
done
