#!/bin/sh
# Used only by docker-compose.job-manager-diagnostics.yml.
set -eu
umask 077
directory=/var/lib/fooddiary/job-manager-crashdumps

verify() {
    test -d "$directory"
    test "$(stat -c '%a:%u:%g' "$directory")" = '700:0:0'
    test -w "$directory"
}

prune() {
    verify
    # This private volume contains only this diagnostic session. Never follow links.
    find "$directory" -maxdepth 1 -type f -name 'job-manager.*' -mmin +10080 -exec rm -f {} \;
    count=0
    find "$directory" -maxdepth 1 -type f -name 'job-manager.*' -print | sort -r |
        while IFS= read -r artifact; do
            count=$((count + 1))
            if [ "$count" -gt 4 ]; then rm -f "$artifact"; fi
        done
}

case "${1:-}" in
    run)
        prune
        # No RLIMIT_FSIZE: application writes must not inherit a diagnostic limit.
        exec dotnet FoodDiary.JobManager.dll
        ;;
    retain)
        prune
        touch "$directory/.ready"
        trap 'exit 0' TERM INT
        while :; do
            sleep 60 &
            wait $! || exit 0
            prune
        done
        ;;
    verify)
        verify
        test -f "$directory/.ready"
        ;;
    prune) prune ;;
    *) echo 'Expected run, retain, verify or prune.' >&2; exit 2 ;;
esac
