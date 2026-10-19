"""Read-only deployment gate for the primary migration history."""

import json
import re
import subprocess
import sys

BASELINE = "20261017130323_InitialBaseline"
HISTORY_SQL = r'''BEGIN READ ONLY;
SELECT to_regclass('public."__EFMigrationsHistory"') IS NOT NULL AS history_exists
\gset
\if :history_exists
SELECT COALESCE(json_agg("MigrationId" ORDER BY "MigrationId"), '[]'::json)
FROM public."__EFMigrationsHistory";
\else
SELECT '[]'::json;
\endif
COMMIT;
'''
HISTORY_COMMAND = [
    "docker", "compose", "--env-file", "/etc/fooddiary/fooddiary.env",
    "exec", "-T", "postgres", "sh", "-c",
    'exec psql -X -q -A -t -v ON_ERROR_STOP=1 -U "$POSTGRES_USER" -d "$POSTGRES_DB"',
]


def baseline_ready(history):
    if not isinstance(history, list) or any(
        not isinstance(item, str)
        or re.fullmatch(r"[0-9]{14}_[A-Za-z0-9_]+", item) is None
        for item in history
    ) or len(set(history)) != len(history):
        raise ValueError("Invalid migration history response")
    return not history or (
        BASELINE in history and all(item >= BASELINE for item in history)
    )


def main(run=subprocess.run, stdout=None, stderr=None):
    stdout = sys.stdout if stdout is None else stdout
    stderr = sys.stderr if stderr is None else stderr
    try:
        result = run(
            HISTORY_COMMAND,
            input=HISTORY_SQL,
            text=True,
            capture_output=True,
            check=True,
            timeout=30,
        )
        ready = baseline_ready(json.loads(result.stdout))
    except (OSError, subprocess.SubprocessError, ValueError):
        print("Unable to verify primary migration history; deployment is blocked.", file=stderr)
        return 2
    if not ready:
        print(
            "Primary database requires explicit baseline adoption before deployment. "
            "Follow docs/backend/BACKEND_MIGRATION_SAFETY.md, including the legacy "
            "Initializer, verified backup and coordinated application cutover. "
            "This check did not apply migrations or stop application writers.",
            file=stderr,
        )
        return 1
    print("Primary migration history permits baseline deployment.", file=stdout)
    return 0


if __name__ == "__main__":
    sys.exit(main())
