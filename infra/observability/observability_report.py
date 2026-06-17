#!/usr/bin/env python3
"""Read-only FoodDiary observability snapshot for host-side diagnosis."""

from __future__ import annotations

import argparse
import datetime as dt
import json
import sys
import time
import urllib.parse
import urllib.request
from typing import Any


PROBLEM_PATTERN = (
    "(?i)(Unhandled exception|OptionsValidationException|BrokerUnreachableException|"
    "Connection refused|NG0103|Failed to determine|critical|fatal)"
)


def get_json(base_url: str, path: str, params: dict[str, str] | None = None) -> dict[str, Any]:
    query = "" if not params else "?" + urllib.parse.urlencode(params)
    with urllib.request.urlopen(base_url.rstrip("/") + path + query, timeout=20) as response:
        return json.load(response)


def ns_to_local_text(timestamp_ns: str) -> str:
    timestamp = int(timestamp_ns) / 1_000_000_000
    return dt.datetime.fromtimestamp(timestamp).isoformat(timespec="seconds")


def print_section(title: str) -> None:
    print()
    print(f"## {title}")


def query_loki(loki_url: str, query: str) -> list[dict[str, Any]]:
    data = get_json(loki_url, "/loki/api/v1/query", {"query": query})
    return data.get("data", {}).get("result", [])


def query_loki_range(loki_url: str, query: str, hours: int, limit: int) -> list[dict[str, Any]]:
    start = str(int((time.time() - hours * 3600) * 1_000_000_000))
    data = get_json(
        loki_url,
        "/loki/api/v1/query_range",
        {
            "query": query,
            "start": start,
            "limit": str(limit),
            "direction": "backward",
        },
    )
    return data.get("data", {}).get("result", [])


def query_prometheus(prometheus_url: str, query: str) -> list[dict[str, Any]]:
    data = get_json(prometheus_url, "/api/v1/query", {"query": query})
    return data.get("data", {}).get("result", [])


def render_loki_vector(title: str, loki_url: str, query: str) -> None:
    print_section(title)
    results = query_loki(loki_url, query)
    if not results:
        print("No data.")
        return

    rows = []
    for item in results:
        value = int(float(item.get("value", [0, "0"])[1]))
        labels = ", ".join(f"{key}={value}" for key, value in sorted(item.get("metric", {}).items()))
        rows.append((value, labels or "{}"))

    for value, labels in sorted(rows, reverse=True):
        print(f"{value:>8}  {labels}")


def render_prometheus_vector(title: str, prometheus_url: str, query: str) -> None:
    print_section(title)
    try:
        results = query_prometheus(prometheus_url, query)
    except Exception as exc:  # noqa: BLE001 - diagnostic script should keep going.
        print(f"Prometheus query unavailable: {exc}")
        return

    if not results:
        print("No data.")
        return

    rows = []
    for item in results:
        value = float(item.get("value", [0, "0"])[1])
        labels = ", ".join(f"{key}={value}" for key, value in sorted(item.get("metric", {}).items()))
        rows.append((value, labels or "{}"))

    for value, labels in sorted(rows, reverse=True):
        print(f"{value:>8.2f}  {labels}")


def render_recent_logs(title: str, loki_url: str, query: str, hours: int, limit: int) -> None:
    print_section(title)
    results = query_loki_range(loki_url, query, hours, limit)
    if not results:
        print("No matching log lines.")
        return

    printed = 0
    for stream in results:
        labels = stream.get("stream", {})
        label_text = " ".join(
            f"{name}={labels[name]}"
            for name in [
                "container",
                "unit",
                "detected_level",
                "logstream",
                "log_source",
                "client_telemetry_category",
            ]
            if labels.get(name)
        )
        for timestamp_ns, line in stream.get("values", []):
            print(f"{ns_to_local_text(timestamp_ns)} {label_text}")
            print(f"  {line[:500]}")
            printed += 1
            if printed >= limit:
                return


def render_prometheus_targets(prometheus_url: str) -> None:
    print_section("Prometheus targets")
    try:
        data = get_json(prometheus_url, "/api/v1/targets", {"state": "active"})
    except Exception as exc:  # noqa: BLE001 - diagnostic script should keep going.
        print(f"Prometheus unavailable: {exc}")
        return

    targets = data.get("data", {}).get("activeTargets", [])
    if not targets:
        print("No active targets.")
        return

    for target in targets:
        print(
            f"{target.get('health', 'unknown'):>7}  "
            f"{target.get('scrapePool', '')}  "
            f"{target.get('scrapeUrl', '')}  "
            f"{target.get('lastError', '')}"
        )


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--loki-url", default="http://127.0.0.1:3100")
    parser.add_argument("--prometheus-url", default="http://127.0.0.1:9090")
    parser.add_argument("--hours", type=int, default=24)
    parser.add_argument("--limit", type=int, default=20)
    args = parser.parse_args()

    print(f"FoodDiary observability report at {dt.datetime.now().isoformat(timespec='seconds')}")
    print(f"Window: last {args.hours}h")

    render_loki_vector(
        "Log volume by container",
        args.loki_url,
        f'sum by (container) (count_over_time({{compose_project="fooddiary"}}[{args.hours}h]))',
    )
    render_loki_vector(
        "Log volume by source",
        args.loki_url,
        f'sum by (log_source) (count_over_time({{compose_project="fooddiary"}}[{args.hours}h]))',
    )
    render_loki_vector(
        "Frontend telemetry by category",
        args.loki_url,
        f'sum by (client_telemetry_category) (count_over_time({{compose_project="fooddiary", log_source="frontend", client_telemetry_category=~".+"}}[{args.hours}h]))',
    )
    render_loki_vector(
        "Problem-like logs by container",
        args.loki_url,
        f'sum by (container) (count_over_time({{compose_project="fooddiary", log_source!="frontend"}} |~ "{PROBLEM_PATTERN}" [{args.hours}h]))',
    )
    render_recent_logs(
        "Recent application problems",
        args.loki_url,
        f'{{compose_project="fooddiary", log_source!="frontend"}} |~ "{PROBLEM_PATTERN}"',
        args.hours,
        args.limit,
    )
    render_recent_logs(
        "Recent frontend telemetry problems",
        args.loki_url,
        '{compose_project="fooddiary", log_source="frontend", client_telemetry_category="client_error"}',
        args.hours,
        args.limit,
    )
    render_loki_vector(
        "Auth client outcomes",
        args.loki_url,
        f'sum by (container) (count_over_time({{compose_project="fooddiary"}} |= "Outcome=client_error" [{args.hours}h]))',
    )
    render_prometheus_vector(
        "External provider outcomes",
        args.prometheus_url,
        "sum by (fooddiary_external_provider, fooddiary_external_provider_operation, "
        "fooddiary_external_provider_outcome) "
        f"(increase(fooddiary_external_provider_requests_total[{args.hours}h]))",
    )
    render_recent_logs(
        "Certbot service logs",
        args.loki_url,
        '{unit=~"certbot.service|snap.certbot.renew.service"}',
        args.hours,
        min(args.limit, 10),
    )
    render_prometheus_targets(args.prometheus_url)

    return 0


if __name__ == "__main__":
    sys.exit(main())
