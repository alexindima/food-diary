#!/bin/sh
set -eu

readonly dump_directory=/var/lib/fooddiary/job-manager-crashdumps
readonly maximum_artifacts=4
readonly maximum_age_days=7
readonly prune_interval_seconds=3600

verify_dump_directory() {
    if [ ! -d "$dump_directory" ]; then
        echo "Job Manager crash-dump directory is unavailable." >&2
        return 1
    fi

    actual_mode_and_owner=$(stat -c '%a:%u:%g' "$dump_directory")
    if [ "$actual_mode_and_owner" != "700:0:0" ]; then
        echo "Job Manager crash-dump directory must be root-owned with mode 0700." >&2
        return 1
    fi

    writability_probe="$dump_directory/.writability-probe.$$"
    if ! : >"$writability_probe"; then
        echo "Job Manager crash-dump directory is not writable." >&2
        return 1
    fi
    if ! rm -f "$writability_probe"; then
        echo "Job Manager crash-dump directory writability probe could not be removed." >&2
        return 1
    fi
}

prune_crash_artifacts() {
    find "$dump_directory" \
        -maxdepth 1 \
        -type f \
        -name 'job-manager.*' \
        -mmin "+$((maximum_age_days * 1440))" \
        -exec rm -f {} \;

    artifact_count=0
    find "$dump_directory" -maxdepth 1 -type f -name 'job-manager.*' -print |
        sort -r |
        while IFS= read -r artifact_path; do
            artifact_count=$((artifact_count + 1))
            if [ "$artifact_count" -gt "$maximum_artifacts" ]; then
                rm -f "$artifact_path"
            fi
        done

    find "$dump_directory" \
        -maxdepth 1 \
        -type f \
        -name 'job-manager.*' \
        -exec chmod 0600 {} \;
}

umask 077
verify_dump_directory
prune_crash_artifacts

(
    while sleep "$prune_interval_seconds"; do
        if ! prune_crash_artifacts; then
            echo "Job Manager crash-artifact retention failed." >&2
        fi
    done
) &

exec "$@"
