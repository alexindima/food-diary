#!/usr/bin/env python3
"""Bounded, read-only production probes. Publish numeric aggregates atomically.

Run as a short-lived root systemd service. Never log subprocess output: it can
contain private diagnostics. Failed components publish success=0 and no invented
measurements. Service/database labels come from the fixed inventory below.
"""
from __future__ import annotations

import argparse
import concurrent.futures
import datetime as dt
import hashlib
import http.client
import json
import math
import os
import pathlib
import re
import socket
import subprocess
import tempfile
import time
import urllib.parse

from external_monitor import probes as public_probes

SERVICES = frozenset([
    "api", "job-manager", "telegram-bot", "client-ssr", "nginx", "postgres",
    "redis", "rabbitmq", "mail-relay", "mail-inbox", "mailrelay-postgres",
    "mailinbox-postgres", "tech-radar", "bugtriage-api", "bugtriage-postgres",
])
DATABASES = {
    "main": "fooddiary-postgres-1", "mailrelay": "fooddiary-mailrelay-postgres-1",
    "mailinbox": "fooddiary-mailinbox-postgres-1",
    "bugtriage": "fooddiary-bugtriage-bugtriage-postgres-1",
}
BACKUPS = ("fooddiary", "mailrelay", "mailinbox")
JOB_INTERVALS = {
    "fasting.notifications": 60, "achievements.evaluation_outbox": 60,
    "billing.cancellation-email": 60, "billing.webhook-inbox": 60,
    "email.outbox": 60, "images.object_deletion_outbox": 60,
    "notifications.web_push_outbox": 60, "atomic-command-receipts.cleanup": 900,
    "weekly_goals.reminders": 900, "billing.renewal": 3600,
    "billing.paddle-notification-recovery": 3600, "images.cleanup": 3600,
    "dietologist.client_task_reminders": 3600, "notifications.cleanup": 86400,
    "users.cleanup": 86400, "users.login_events_cleanup": 86400,
    "users.sessions_cleanup": 86400, "marketing.attribution_cleanup": 86400,
    "fasting.telemetry_cleanup": 86400,
}
POSTGRES_SQL = """
BEGIN READ ONLY;
SET LOCAL statement_timeout = '3000ms';
SELECT json_build_object(
 'connections', (SELECT count(*) FROM pg_stat_activity WHERE backend_type = 'client backend'),
 'connections_max', current_setting('max_connections')::int,
 'active', (SELECT count(*) FROM pg_stat_activity WHERE state = 'active' AND pid <> pg_backend_pid()),
 'waiting_locks', (SELECT count(*) FROM pg_stat_activity WHERE wait_event_type = 'Lock'),
 'idle_in_transaction', (SELECT count(*) FROM pg_stat_activity WHERE state LIKE 'idle in transaction%'),
 'oldest_transaction_seconds', (SELECT coalesce(max(extract(epoch FROM clock_timestamp()-xact_start)),0) FROM pg_stat_activity WHERE pid <> pg_backend_pid()),
 'deadlocks_total', (SELECT coalesce(sum(deadlocks),0) FROM pg_stat_database),
 'commits_total', (SELECT coalesce(sum(xact_commit),0) FROM pg_stat_database),
 'rollbacks_total', (SELECT coalesce(sum(xact_rollback),0) FROM pg_stat_database),
 'size_bytes', pg_database_size(current_database()));
COMMIT;
"""


class UnixHTTPConnection(http.client.HTTPConnection):
    def connect(self):
        self.sock = socket.socket(socket.AF_UNIX, socket.SOCK_STREAM)
        self.sock.settimeout(self.timeout)
        self.sock.connect("/var/run/docker.sock")


def docker_get(path):
    connection = UnixHTTPConnection("localhost", timeout=4)
    try:
        connection.request("GET", "/v1.44" + path)
        response = connection.getresponse()
        if response.status != 200:
            raise RuntimeError("Docker probe failed")
        body = response.read(4 * 1024 * 1024 + 1)
        if len(body) > 4 * 1024 * 1024:
            raise ValueError("Docker response limit")
        return json.loads(body)
    finally:
        connection.close()


def run(args, timeout=8):
    result = subprocess.run(args, capture_output=True, text=True, timeout=timeout, check=False)
    if result.returncode:
        raise RuntimeError("Probe command failed")
    if len(result.stdout) > 1024 * 1024:
        raise ValueError("Probe response limit")
    return result.stdout


class Metrics:
    def __init__(self):
        self.lines = []
        self.types = set()

    def add(self, name, value, kind="gauge", **labels):
        number = float(value)
        if not math.isfinite(number) or number < 0:
            raise ValueError("Invalid observation")
        name = "fooddiary_" + name
        if name not in self.types:
            help_text = {
                "fooddiary_collector_component_success": "One for a completed valid probe, zero on failure; failed observations are omitted.",
                "fooddiary_container_restarts": "Current Docker restart-count snapshot; resets when the container is replaced.",
                "fooddiary_backup_verified": "One when the newest completed dump matches its SHA256 manifest; does not prove a full restore.",
                "fooddiary_backup_last_success_timestamp_seconds": "Modification time of the newest checksum-verified dump; zero for a corrupt newest dump.",
                "fooddiary_public_probe_tls_seconds_remaining": "Remaining verified public TLS certificate lifetime in seconds.",
            }.get(name, "FoodDiary " + name.removeprefix("fooddiary_").replace("_", " ") + ".")
            self.lines.append(f"# HELP {name} {help_text}")
            self.lines.append(f"# TYPE {name} {kind}")
            self.types.add(name)
        tags = ",".join(f"{k}={json.dumps(str(v), ensure_ascii=True)}" for k, v in sorted(labels.items()))
        self.lines.append(f"{name}" + ("{" + tags + "}" if tags else "") + f" {number:.12g}")

    def render(self):
        return "\n".join(self.lines) + "\n"

    def merge(self, other):
        for line in other.lines:
            if line.startswith("# HELP ") and line.split()[2] in self.types:
                continue
            if line.startswith("# TYPE "):
                name = line.split()[2]
                if name in self.types:
                    continue
                self.types.add(name)
            self.lines.append(line)


def container_observations(info, stats):
    state = info["State"]
    memory = stats.get("memory_stats", {})
    if "usage" not in memory or "total_usage" not in stats.get("cpu_stats", {}).get("cpu_usage", {}):
        raise ValueError("Incomplete Docker resource observation")
    cache = memory.get("stats", {}).get("inactive_file", memory.get("stats", {}).get("cache", 0))
    # Python 3.10 accepts microseconds; Docker reports nanosecond precision.
    started_text = re.sub(r"(\.\d{6})\d+", r"\1", state["StartedAt"])
    started = dt.datetime.fromisoformat(started_text.replace("Z", "+00:00")).timestamp()
    return {
        "running": int(state["Running"]),
        "healthy": int(state["Running"] and state.get("Health", {}).get("Status", "healthy") == "healthy"),
        "restarts": info["RestartCount"],
        "oom_killed": int(state.get("OOMKilled", False)),
        "started_timestamp_seconds": started,
        "memory_working_set_bytes": max(0, memory.get("usage", 0) - cache),
        "memory_limit_bytes": memory.get("limit", 0),
        "cpu_usage_seconds_total": stats.get("cpu_stats", {}).get("cpu_usage", {}).get("total_usage", 0) / 1e9,
        "cpu_limit_cores": info.get("HostConfig", {}).get("NanoCpus", 0) / 1e9,
    }


def collect_containers(metrics):
    inventory = docker_get("/containers/json?all=true")
    # Aggregate blue/green generations under their stable service. Completed
    # migration/init containers and the legacy static image are not workloads.
    selected = {}
    for item in inventory:
        labels = item.get("Labels", {})
        project = labels.get("com.docker.compose.project", "")
        if project not in {"fooddiary", "fooddiary-bugtriage"}:
            continue
        service = labels.get("com.docker.compose.service", "")
        if project == "fooddiary-bugtriage" and service == "api":
            service = "bugtriage-api"
        service = service.removesuffix("-green")
        if service not in SERVICES:
            continue
        if item["State"] == "running":
            selected.setdefault(service, []).append(item["Id"])
    def observe(item):
        service, container_id = item
        info = docker_get(f"/containers/{container_id}/json")
        stats = docker_get(f"/containers/{container_id}/stats?stream=false&one-shot=true")
        return service, container_observations(info, stats)
    totals = {}
    with concurrent.futures.ThreadPoolExecutor(max_workers=4) as executor:
        for service, values in executor.map(observe, [(s, i) for s, ids in selected.items() for i in ids]):
            previous = totals.setdefault(service, {})
            for key, value in values.items():
                if key == "started_timestamp_seconds":
                    previous[key] = max(previous.get(key, 0), value)
                else:
                    previous[key] = previous.get(key, 0) + value
    for service in sorted(SERVICES):
        metrics.add("container_running", totals.get(service, {}).get("running", 0), service=service)
        for key, value in totals.get(service, {}).items():
            if key != "running":
                # Restarts are a snapshot gauge: replacing a container resets
                # Docker's count without proving that the service recovered.
                kind = "counter" if key == "cpu_usage_seconds_total" else "gauge"
                metrics.add("container_" + key, value, kind, service=service)


def collect_postgres(metrics, alias, container):
    command = 'exec psql --no-password --username "$POSTGRES_USER" --dbname "$POSTGRES_DB" -XqAt -c "$1"'
    values = json.loads(run(["docker", "exec", container, "sh", "-c", command, "probe", POSTGRES_SQL]))
    for key, value in values.items():
        metrics.add("postgres_" + key, value, "counter" if key.endswith("_total") else "gauge", database=alias)


def parse_redis(text):
    return dict(line.split(":", 1) for line in text.splitlines() if line and not line.startswith("#") and ":" in line)


def collect_redis(metrics):
    info = parse_redis(run(["docker", "exec", "fooddiary-redis-1", "redis-cli", "--raw", "INFO"]))
    fields = {
        "used_memory": "memory_used_bytes", "maxmemory": "memory_max_bytes",
        "connected_clients": "connected_clients", "blocked_clients": "blocked_clients",
        "evicted_keys": "evicted_keys_total", "keyspace_hits": "keyspace_hits_total",
        "keyspace_misses": "keyspace_misses_total", "total_commands_processed": "commands_total",
        "rejected_connections": "rejected_connections_total", "uptime_in_seconds": "uptime_seconds",
    }
    for source, name in fields.items():
        metrics.add("redis_" + name, info[source], "counter" if name.endswith("_total") else "gauge")


def collect_rabbitmq(metrics):
    queues = json.loads(run(["docker", "exec", "fooddiary-rabbitmq-1", "rabbitmqctl", "-q", "--timeout", "5",
                             "list_queues", "--formatter", "json", "messages_ready", "messages_unacknowledged", "consumers"], timeout=10))
    metrics.add("rabbitmq_queues", len(queues))
    for key in ["messages_ready", "messages_unacknowledged", "consumers"]:
        metrics.add("rabbitmq_" + key, sum(int(q[key]) for q in queues))
    metrics.add("rabbitmq_queues_without_consumers", sum(int(q["messages_ready"]) > 0 and int(q["consumers"]) == 0 for q in queues))


def backup_observation(root, label):
    directories = sorted((p for p in root.iterdir() if p.is_dir() and len(p.name) == 16 and p.name.endswith("Z")), reverse=True)
    # Only the newest completed set counts. A missing/corrupt newest member must
    # never be hidden by falling back to an older successful set.
    if not directories:
        raise FileNotFoundError("No completed backup")
    directory = directories[0]
    dump = directory / (label + ".dump")
    manifest = directory / "SHA256SUMS"
    expected = {}
    for line in manifest.read_text(encoding="ascii").splitlines():
        digest, filename = line.split(maxsplit=1)
        expected[filename.lstrip("*")] = digest
    digest = hashlib.sha256()
    with dump.open("rb") as stream:
        for chunk in iter(lambda: stream.read(1024 * 1024), b""):
            digest.update(chunk)
    valid = dump.stat().st_size > 0 and digest.hexdigest() == expected.get(dump.name)
    return {"size_bytes": dump.stat().st_size, "verified": int(valid),
            "last_success_timestamp_seconds": dump.stat().st_mtime if valid else 0}


def collect_backup(metrics, root, label):
    for key, value in backup_observation(root, label).items():
        metrics.add("backup_" + key, value, database=label)


def collect_public(metrics):
    for observation in public_probes():
        for field in ["success", "duration_seconds"]:
            metrics.add("public_probe_" + field, observation[field], target=observation["target"])
        metrics.add("public_probe_tls_seconds_remaining", observation["tls_days_remaining"]*86400, target=observation["target"])


def atomic_write(path, text):
    path.parent.mkdir(parents=True, exist_ok=True)
    descriptor, temporary = tempfile.mkstemp(prefix=".fooddiary-", dir=path.parent)
    try:
        with os.fdopen(descriptor, "w", encoding="ascii", newline="\n") as stream:
            stream.write(text)
            stream.flush()
            os.fsync(stream.fileno())
        os.chmod(temporary, 0o644)
        os.replace(temporary, path)
    finally:
        if os.path.exists(temporary):
            os.unlink(temporary)


def collect(root):
    metrics = Metrics()
    probes = [("containers", collect_containers), ("redis", collect_redis), ("rabbitmq", collect_rabbitmq), ("public", collect_public)]
    probes += [("postgres_" + a, lambda m, a=a, c=c: collect_postgres(m, a, c)) for a, c in DATABASES.items()]
    probes += [("backup_" + b, lambda m, b=b: collect_backup(m, root, b)) for b in BACKUPS]
    def perform(item):
        component, probe = item
        partial = Metrics()
        try:
            probe(partial)
        except (OSError, ValueError, RuntimeError, KeyError, subprocess.SubprocessError, http.client.HTTPException):
            return component, None
        else:
            return component, partial
    with concurrent.futures.ThreadPoolExecutor(max_workers=4) as executor:
        for component, partial in executor.map(perform, probes):
            if partial is not None:
                metrics.merge(partial)
            metrics.add("collector_component_success", int(partial is not None), component=component)
    for job, interval in JOB_INTERVALS.items():
        metrics.add("job_expected_interval_seconds", interval, fooddiary_job_name=job)
    metrics.add("collector_last_run_timestamp_seconds", time.time())
    return metrics.render()


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--output", type=pathlib.Path, default=pathlib.Path("/var/lib/prometheus/node-exporter/fooddiary.prom"))
    parser.add_argument("--backup-root", type=pathlib.Path, default=pathlib.Path("/var/backups/fooddiary/postgres"))
    args = parser.parse_args()
    started = time.monotonic()
    text = collect(args.backup_root)
    text += f"# HELP fooddiary_collector_duration_seconds Wall-clock duration of the completed probe cycle.\n# TYPE fooddiary_collector_duration_seconds gauge\nfooddiary_collector_duration_seconds {time.monotonic()-started:.6f}\n"
    atomic_write(args.output, text)


if __name__ == "__main__":
    main()
