"""Fixed-window audit entrypoint; the caller supplies its authenticated GET transport."""

from collections import Counter
from datetime import datetime, timedelta, timezone
import json
from pathlib import Path
import re
from uuid import UUID

from collect_grafana import collect_audit, utc_nanoseconds


LEVELS = ("trace", "debug", "info", "warning", "error", "critical", "unknown")


def run_audit(get_json, run_path, plan):
    """Read the original private run.json; never create a new window or publish it.

    plan contains collect_audit keyword arguments verified against current sources.
    No run fields other than the UUID and fixed interval enter the result.
    """
    run = json.loads(Path(run_path).read_text(encoding="utf-8-sig"))
    run_id = str(UUID(run["id"]))
    collection = collect_audit(get_json, run["fromUtc"], run["toUtc"], **plan)
    return {"runId": run_id, "collection": collection,
            "report": render_report(collection)}


def render_report(collection):
    """Render sanitized collector aggregates; no log excerpts or incident guesses."""
    start, end = collection["fromUtc"], collection["toUtc"]
    if utc_nanoseconds(start) >= utc_nanoseconds(end):
        raise ValueError("Invalid collection interval")
    local = timezone(timedelta(hours=4), "Asia/Tbilisi")
    start_local = datetime.fromisoformat(start.replace("Z", "+00:00")).astimezone(local)
    end_local = datetime.fromisoformat(end.replace("Z", "+00:00")).astimezone(local)
    totals = Counter()
    source_count = 0
    for source in collection["sources"]:
        if source["type"] != "loki":
            continue
        source_count += 1
        for row in source["counts"]:
            service, level, count = row["service"], row["level"], row["count"]
            if (not isinstance(service, str) or not re.fullmatch(r"[A-Za-z0-9_.-]{1,80}", service)
                    or level not in LEVELS or type(count) is not int or count < 0):
                raise ValueError("Invalid sanitized aggregate")
            totals[(service, level)] += count
    complete = collection["collectionComplete"] is True
    start_utc = start_local.astimezone(timezone.utc).isoformat()
    end_utc = end_local.astimezone(timezone.utc).isoformat()
    lines = [f"UTC window: [{start_utc}, {end_utc})",
             f"Asia/Tbilisi: [{start_local.isoformat()}, {end_local.isoformat()})",
             f"Configured collection complete: {'yes' if complete else 'no'}.",
             f"Loki sources: {source_count}; observed rows (lower bound): {sum(totals.values())}.",
             f"Collection gaps: {len(collection['gaps'])}.", "",
             "| Service | " + " | ".join(LEVELS) + " | Total |",
             "| --- | " + " | ".join("---:" for _ in LEVELS) + " | ---: |"]
    for service in sorted({service for service, _ in totals}):
        counts = [totals[(service, level)] for level in LEVELS]
        lines.append(f"| {service} | " + " | ".join(map(str, counts)) + f" | {sum(counts)} |")
    lines.extend(["", "Prometheus results are instant snapshots, not interval health.",
                  "Host correlation, interval metrics and incident classification require separate evidence."])
    return "\n".join(lines) + "\n"
