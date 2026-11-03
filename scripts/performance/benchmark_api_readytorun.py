"""Compare fresh production API processes against an owned, disposable database.

The result measures backend startup and first API requests, not browser rendering
or a cold operating-system cache. Only resources bearing this run's label may be
reset or removed. Credentials and browser cookies stay in the ignored workspace.
"""

import argparse
from concurrent.futures import ThreadPoolExecutor
from datetime import datetime, timezone
import hashlib
import json
import os
from pathlib import Path
import re
import secrets
import statistics
import struct
import subprocess
import time
import urllib.error
import urllib.request
import uuid


ROOT = Path(__file__).resolve().parents[2]
LABEL = "fooddiary.performance.run"
FIRST_WAVE = [
    "/api/v1/notifications/unread-count",
    "/api/v1/notifications",
    "/api/v1/notifications/push/config",
    "/api/v1/users/privacy",
    "/api/v1/dashboard",
]
SECTIONS = ["/api/v1/meals/overview", "/api/v1/products/overview"]


def command(*args, env=None, timeout=180):
    result = subprocess.run(args, cwd=ROOT, env=env, capture_output=True,
                            text=True, timeout=timeout)
    if result.returncode:
        # Do not echo command arguments, environment values or HTTP payloads.
        raise RuntimeError(f"{args[0]} failed (exit {result.returncode})")
    return result.stdout.strip()


def inspect(kind, name):
    return json.loads(command("docker", kind, "inspect", name))[0]


def require_local_engine():
    endpoint = os.environ.get("DOCKER_HOST")
    if os.environ.get("DOCKER_CONTEXT") or not endpoint:
        context = json.loads(command("docker", "context", "inspect"))[0]
        endpoint = context["Endpoints"]["docker"]["Host"]
    if not endpoint.startswith(("npipe://", "unix://")):
        raise RuntimeError("Benchmark requires a local Docker socket; remote engines are not supported")
    if command("docker", "info", "--format", "{{.OSType}}") != "linux":
        raise RuntimeError("Benchmark requires Linux containers")


def owned(state, kind, name):
    item = inspect(kind, name)
    labels = item.get("Labels") if kind in ("network", "volume") else item["Config"].get("Labels")
    if (labels or {}).get(LABEL) != state["runId"]:
        raise RuntimeError("Refusing to modify a resource owned by another run")
    return item


def remove_container(state, name):
    owned(state, "container", name)
    command("docker", "rm", "-f", "-v", name)


def port(name, container_port):
    item = inspect("container", name)
    bindings = item["NetworkSettings"]["Ports"][f"{container_port}/tcp"]
    if len(bindings) != 1 or bindings[0]["HostIp"] != "127.0.0.1":
        raise RuntimeError("Expected a loopback-only port binding")
    return int(bindings[0]["HostPort"])


def request(base, path, cookie="", method="GET", body=None, expected_status=200):
    headers = {"X-Requested-With": "XMLHttpRequest"}
    if cookie:
        headers["Cookie"] = cookie
    if body is not None:
        headers["Content-Type"] = "application/json"
        body = json.dumps(body).encode()
    started = time.perf_counter()
    outgoing = urllib.request.Request(base + path, data=body, headers=headers, method=method)
    with urllib.request.urlopen(outgoing, timeout=60) as response:
        payload = response.read()
        if response.status != expected_status:
            raise RuntimeError(f"Unexpected HTTP status {response.status} for {path}")
        return {"path": path, "status": response.status,
                "milliseconds": (time.perf_counter() - started) * 1000,
                "bytes": len(payload)}, response.headers


def wait_ready(base, path="/health/ready", timeout=120, container=None):
    deadline = time.monotonic() + timeout
    while time.monotonic() < deadline:
        if container and not inspect("container", container)["State"]["Running"]:
            raise RuntimeError("Owned API exited before readiness; inspect its local log")
        try:
            with urllib.request.urlopen(base + path, timeout=5) as response:
                if response.status == 200:
                    return
        except (OSError, urllib.error.URLError, TimeoutError):
            pass
        time.sleep(0.1)
    raise RuntimeError("Owned service did not become ready")


def sql(state, statement):
    owned(state, "container", state["postgres"])
    return command("docker", "exec", state["postgres"], "psql", "-U", "postgres",
                   "-d", "postgres", "-v", "ON_ERROR_STOP=1", "-At", "-c", statement)


def environment(state, database):
    return {
        "ASPNETCORE_ENVIRONMENT": "Production",
        "ConnectionStrings__DefaultConnection": f"Host=postgres;Database={database};Username=postgres;Password={state['dbPassword']}",
        "ConnectionStrings__Redis": "redis:6379,abortConnect=false",
        "Jwt__SecretKey": state["jwtKey"],
        "Jwt__Issuer": "FoodDiary.ReadyToRun.Benchmark",
        "Jwt__Audience": "FoodDiary.ReadyToRun.Benchmark",
        "S3__AccessKeyId": "benchmark-access",
        "S3__SecretAccessKey": state["storageKey"],
        "S3__Region": "us-east-1",
        "S3__Bucket": "benchmark",
        "S3__StagingBucket": "benchmark-staging",
        "S3__ServiceUrl": "http://storage:9000",
        "S3__PublicBaseUrl": "http://storage:9000",
        "S3__AllowInsecureHttp": "true",
        "S3__AllowPublicImageAccess": "true",
        "Cors__Origins__0": "http://localhost",
        "Cors__Origins__1": "http://127.0.0.1",
        "MailRelayClient__BaseUrl": "https://mail-relay.example.test",
        "MailRelayClient__ApiKey": state["providerKey"],
        "MailInboxClient__BaseUrl": "https://mail-inbox.example.test",
        "MailInboxClient__MetadataApiKey": state["providerKey"] + "a",
        "MailInboxClient__ContentApiKey": state["providerKey"] + "b",
        "MailInboxClient__StateApiKey": state["providerKey"] + "c",
        "DataProtection__ApplicationName": "FoodDiary.ReadyToRun.Benchmark",
        "DataProtection__KeyRingPath": "/keys",
        "OpenTelemetry__Otlp__Endpoint": "http://collector:4317",
        "OTEL_METRIC_EXPORT_INTERVAL": "1000",
        "RateLimiting__Auth__PermitLimit": "1000",
        "RateLimiting__PasswordLogin__PermitLimit": "1000",
    }


def launch_api(state, workspace, image, database, name):
    owned(state, "volume", state["keyVolume"])
    env_file = workspace / "api.env"
    env_file.write_text("".join(f"{k}={v}\n" for k, v in environment(state, database).items()), encoding="utf-8")
    command("docker", "run", "-d", "--name", name, "--label", f"{LABEL}={state['runId']}",
            "--network", state["network"], "--cpus", "2", "--memory", "768m",
            "--publish", "127.0.0.1::5000", "--env-file", str(env_file),
            "--mount", f"type=volume,source={state['keyVolume']},target=/keys", image)
    return f"http://127.0.0.1:{port(name, 5000)}"


def prepare(args, workspace):
    if (workspace / "private.json").exists():
        raise RuntimeError("Workspace already contains a prepared run")
    run_id = uuid.uuid4().hex[:12]
    state = {"runId": run_id, "network": f"fd-r2r-{run_id}",
             "date": datetime.now(timezone.utc).date().isoformat(),
             "postgres": f"fd-r2r-{run_id}-postgres", "redis": f"fd-r2r-{run_id}-redis",
             "storage": f"fd-r2r-{run_id}-storage",
             "keyVolume": f"fd-r2r-{run_id}-keys",
             "collector": f"fd-r2r-{run_id}-collector", "fixtureDb": f"r2r_fixture_{run_id}",
             "dbPassword": secrets.token_urlsafe(24), "accountPassword": "R2r!" + secrets.token_urlsafe(24),
             "email": f"r2r-{run_id}@example.test", "jwtKey": secrets.token_hex(32),
             "storageKey": secrets.token_hex(24), "providerKey": secrets.token_hex(24)}
    save_private(workspace, state)
    command("docker", "network", "create", "--label", f"{LABEL}={run_id}", state["network"])
    command("docker", "volume", "create", "--label", f"{LABEL}={run_id}", state["keyVolume"])
    command("docker", "run", "-d", "--name", state["postgres"], "--label", f"{LABEL}={run_id}",
            "--network", state["network"], "--network-alias", "postgres", "--publish", "127.0.0.1::5432",
            "--env", f"POSTGRES_PASSWORD={state['dbPassword']}", "--env", f"POSTGRES_DB={state['fixtureDb']}", "postgres:18-alpine")
    command("docker", "run", "-d", "--name", state["redis"], "--label", f"{LABEL}={run_id}",
            "--network", state["network"], "--network-alias", "redis", "redis:7-alpine")
    command("docker", "run", "-d", "--name", state["storage"], "--label", f"{LABEL}={run_id}",
            "--network", state["network"], "--network-alias", "storage", "--publish", "127.0.0.1::9000",
            "--env", "MINIO_ROOT_USER=benchmark-access", "--env", f"MINIO_ROOT_PASSWORD={state['storageKey']}",
            "quay.io/minio/minio@sha256:14cea493d9a34af32f524e538b8346cf79f3321eff8e708c1e2960462bd8936e",
            "server", "/data", "--address", ":9000")
    wait_ready(f"http://127.0.0.1:{port(state['storage'],9000)}", "/minio/health/ready")
    command("docker", "run", "--rm", "--label", f"{LABEL}={run_id}", "--network", state["network"],
            "--env", "BENCH_ACCESS=benchmark-access", "--env", f"BENCH_SECRET={state['storageKey']}",
            "--entrypoint", "sh", "quay.io/minio/mc@sha256:a7fe349ef4bd8521fb8497f55c6042871b2ae640607cf99d9bede5e9bdf11727",
            "-c", 'mc alias set bench http://storage:9000 "$BENCH_ACCESS" "$BENCH_SECRET" >/dev/null && mc mb --ignore-existing bench/benchmark bench/benchmark-staging')
    configuration = workspace / "collector.yaml"
    configuration.write_text("""receivers:
  otlp:
    protocols:
      grpc:
        endpoint: 0.0.0.0:4317
processors:
  batch:
    timeout: 1s
exporters:
  prometheus:
    endpoint: 0.0.0.0:8889
  debug:
    verbosity: basic
service:
  telemetry:
    logs:
      level: warn
  pipelines:
    metrics:
      receivers: [otlp]
      processors: [batch]
      exporters: [prometheus]
    traces:
      receivers: [otlp]
      processors: [batch]
      exporters: [debug]
""", encoding="utf-8")
    command("docker", "run", "-d", "--name", state["collector"], "--label", f"{LABEL}={run_id}",
            "--network", state["network"], "--network-alias", "collector", "--publish", "127.0.0.1::8889",
            "--mount", f"type=bind,source={configuration},target=/etc/otelcol-contrib/config.yaml,readonly",
            "otel/opentelemetry-collector-contrib:0.141.0")
    for attempt in range(60):
        result = subprocess.run(["docker", "exec", state["postgres"], "pg_isready", "-U", "postgres"], capture_output=True)
        if result.returncode == 0:
            break
        time.sleep(1)
    else:
        raise RuntimeError("Owned PostgreSQL did not start")
    env = os.environ.copy()
    for key, value in environment(state, state["fixtureDb"]).items():
        env["FOODDIARY_" + key] = value
    env["FOODDIARY_ConnectionStrings__DefaultConnection"] = f"Host=127.0.0.1;Port={port(state['postgres'],5432)};Database={state['fixtureDb']};Username=postgres;Password={state['dbPassword']}"
    env["FOODDIARY_ConnectionStrings__Redis"] = ""
    env["FOODDIARY_InitialAdmin__Email"] = state["email"]
    env["FOODDIARY_InitialAdmin__Password"] = state["accountPassword"]
    env["FOODDIARY_InitialAdmin__BootstrapTimeoutSeconds"] = "120"
    env["DOTNET_ENVIRONMENT"] = "Production"
    initializer = subprocess.run(["dotnet", str(Path(args.initializer).resolve()), "update"],
                                 cwd=ROOT, env=env, capture_output=True, text=True, timeout=180)
    (workspace / "initializer.log").write_text(initializer.stdout + initializer.stderr, encoding="utf-8")
    if initializer.returncode:
        raise RuntimeError("Owned initializer failed; inspect the local initializer log")
    name = f"fd-r2r-{run_id}-setup"
    try:
        base = launch_api(state, workspace, args.baseline_image, state["fixtureDb"], name)
        wait_ready(base, container=name)
        _, headers = request(base, "/api/v1/auth/login", method="POST",
                             body={"email": state["email"], "password": state["accountPassword"]})
        cookies = [line.split(";", 1)[0] for line in headers.get_all("Set-Cookie", [])]
        if not cookies:
            raise RuntimeError("Fixture login did not return a browser cookie")
        state["cookie"] = "; ".join(cookies)
        request(base, "/api/v1/users/info", state["cookie"])
        configure_fixture_privacy(base, state["cookie"])
        save_private(workspace, state)
    finally:
        (workspace / "setup-api.log").write_text(command("docker", "logs", name), encoding="utf-8")
        remove_container(state, name)
    print("Prepared isolated fixture, production API cookie and telemetry collector", flush=True)


def configure_fixture_privacy(base, cookie):
    outgoing = urllib.request.Request(base + "/api/v1/users/privacy", headers={"Cookie": cookie})
    with urllib.request.urlopen(outgoing, timeout=30) as response:
        privacy = json.load(response)
    request(base, "/api/v1/users/privacy", cookie, method="PUT",
            body={"purpose": 1, "granted": True, "noticeVersion": privacy["noticeVersion"]},
            expected_status=204)


def save_private(workspace, state):
    path = workspace / "private.json"
    path.write_text(json.dumps(state, indent=2), encoding="utf-8")
    path.chmod(0o600)


def memory(name):
    text = command("docker", "exec", name, "cat", "/proc/1/status")
    return {key: int(re.search(rf"^{key}:\s+(\d+)", text, re.M)[1]) * 1024 for key in ("VmRSS", "VmHWM")}


def telemetry(state):
    base = f"http://127.0.0.1:{port(state['collector'],8889)}"
    time.sleep(2.2)
    with urllib.request.urlopen(base + "/metrics", timeout=5) as response:
        text = response.read().decode()
    result = {}
    for metric in ("dotnet_jit_compilation_time_seconds_total", "dotnet_jit_compiled_methods_total", "dotnet_gc_pause_time_seconds_total"):
        values = [float(line.rsplit(" ", 1)[1]) for line in text.splitlines()
                  if line.startswith(metric + "{") and 'FoodDiary.Web.Api' in line]
        if not values:
            raise RuntimeError("Runtime telemetry missing; do not interpret missing samples as zero")
        result[metric] = sum(values)
    return result


def ready_to_run_header(path):
    data = path.read_bytes()
    pe = struct.unpack_from("<I", data, 0x3C)[0]
    if data[pe:pe + 4] != b"PE\0\0":
        raise RuntimeError("Expected a managed PE assembly")
    section_count = struct.unpack_from("<H", data, pe + 6)[0]
    optional_size = struct.unpack_from("<H", data, pe + 20)[0]
    optional = pe + 24
    magic = struct.unpack_from("<H", data, optional)[0]
    directory = optional + (112 if magic == 0x20B else 96)
    cli_rva = struct.unpack_from("<I", data, directory + 14 * 8)[0]

    def offset(rva):
        for index in range(section_count):
            section = optional + optional_size + index * 40
            virtual_size, address, raw_size, raw_pointer = struct.unpack_from("<IIII", data, section + 8)
            if address <= rva < address + max(virtual_size, raw_size):
                return raw_pointer + rva - address
        raise RuntimeError("Managed assembly RVA does not map to a section")

    native_rva, native_size = struct.unpack_from("<II", data, offset(cli_rva) + 64)
    if not native_rva:
        return False
    if native_size < 16 or data[offset(native_rva):offset(native_rva) + 4] != b"RTR\0":
        raise RuntimeError("Unexpected managed native header")
    return True


def verify_images(args, workspace, state):
    for variant, image, expected in (("baseline", args.baseline_image, False),
                                     ("readytorun", args.candidate_image, True)):
        container = command("docker", "create", "--label", f"{LABEL}={state['runId']}", image)
        try:
            path = workspace / (variant + ".dll")
            command("docker", "cp", container + ":/app/FoodDiary.Web.Api.dll", str(path))
            if ready_to_run_header(path) != expected:
                raise RuntimeError(f"{variant} does not have the expected ReadyToRun state")
        finally:
            remove_container(state, container)


def measure(args, workspace, state):
    for key in ("postgres", "redis", "collector"):
        owned(state, "container", state[key])
    owned(state, "network", state["network"])
    verify_images(args, workspace, state)
    fixture_date = state.get("date", datetime.now(timezone.utc).date().isoformat())
    first_wave = [path + f"?date={fixture_date}&timeZoneOffsetMinutes=0" if path.endswith("/dashboard") else path
                  for path in FIRST_WAVE]
    results = {"schemaVersion": 1, "sourceSha": command("git", "rev-parse", "HEAD"),
               "buildInputs": {str(path.relative_to(ROOT)): hashlib.sha256(path.read_bytes()).hexdigest()
                               for path in (ROOT / "Directory.Build.props", ROOT / "FoodDiary.Web.Api/Dockerfile")},
               "readyToRunHeadersVerified": True,
               "startedAtUtc": datetime.now(timezone.utc).isoformat(), "cpuLimit": 2, "memoryLimitMiB": 768,
               "scenario": "Fresh API process, existing authenticated browser session, parallel startup reads, first section reads, then warm repeats",
               "persistentDataProtectionKeyRing": True,
               "limitations": ["Backend timings; excludes browser download, rendering and Internet latency", "Operating-system and PostgreSQL caches are not cleared", "Synthetic admin fixture with no food history", "OTLP exports every second for both variants"],
               "images": {name: {"id": inspect("image", image)["Id"], "bytes": inspect("image", image)["Size"]}
                          for name, image in (("baseline", args.baseline_image), ("readytorun", args.candidate_image))}, "trials": []}
    for repetition in range(args.repetitions):
        variants = [("baseline", args.baseline_image), ("readytorun", args.candidate_image)]
        if repetition % 2:
            variants.reverse()
        for variant, image in variants:
            database = f"r2r_trial_{state['runId']}_{repetition}_{variant}"
            name = f"fd-r2r-{state['runId']}-trial"
            sql(state, f'CREATE DATABASE "{database}" TEMPLATE "{state["fixtureDb"]}"')
            owned(state, "container", state["redis"])
            command("docker", "exec", state["redis"], "redis-cli", "FLUSHDB")
            command("docker", "restart", state["collector"])
            wait_ready(f"http://127.0.0.1:{port(state['collector'],8889)}", "/metrics")
            started = time.perf_counter()
            try:
                base = launch_api(state, workspace, image, database, name)
                wait_ready(base, container=name)
                ready_ms = (time.perf_counter() - started) * 1000
                cold_started = time.perf_counter()
                session, _ = request(base, "/api/v1/auth/session", state["cookie"], method="POST")
                with ThreadPoolExecutor(max_workers=len(first_wave)) as pool:
                    cold = list(pool.map(lambda path: request(base, path, state["cookie"])[0], first_wave))
                first_open_ms = (time.perf_counter() - cold_started) * 1000
                sections = [request(base, path, state["cookie"])[0] for path in SECTIONS]
                cold_memory = memory(name)
                cold_runtime = telemetry(state)
                warm = [[request(base, path, state["cookie"])[0] for path in first_wave + SECTIONS] for _ in range(3)]
                trial = {"variant": variant, "repetition": repetition + 1, "readyMs": ready_ms,
                         "session": session, "firstOpenMs": first_open_ms, "cold": cold,
                         "sections": sections, "coldMemory": cold_memory, "coldRuntime": cold_runtime,
                         "warm": warm, "warmMemory": memory(name)}
                results["trials"].append(trial)
                (workspace / "results.json").write_text(json.dumps(results, indent=2), encoding="utf-8")
                print(f"{variant} #{repetition+1}: ready={ready_ms:.0f}ms first-open={first_open_ms:.0f}ms RSS={cold_memory['VmRSS']/1048576:.1f}MiB", flush=True)
            finally:
                (workspace / f"{variant}-{repetition+1}.log").write_text(command("docker", "logs", name), encoding="utf-8")
                remove_container(state, name)
                sql(state, f'DROP DATABASE "{database}" WITH (FORCE)')
    results["summary"] = {variant: {"firstOpenMedianMs": statistics.median(t["firstOpenMs"] for t in results["trials"] if t["variant"] == variant),
                                      "readyMedianMs": statistics.median(t["readyMs"] for t in results["trials"] if t["variant"] == variant)}
                          for variant in ("baseline", "readytorun")}
    (workspace / "results.json").write_text(json.dumps(results, indent=2), encoding="utf-8")
    print(json.dumps(results["summary"]), flush=True)


def cleanup(workspace, state):
    containers = command("docker", "ps", "-aq", "--filter", f"label={LABEL}={state['runId']}").splitlines()
    for item in containers:
        remove_container(state, item)
    owned(state, "network", state["network"])
    command("docker", "network", "rm", state["network"])
    owned(state, "volume", state["keyVolume"])
    command("docker", "volume", "rm", state["keyVolume"])
    for name in ("private.json", "api.env"):
        (workspace / name).unlink(missing_ok=True)
    print("Removed only this benchmark's labelled containers and network", flush=True)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("phase", choices=("prepare", "measure", "cleanup"))
    parser.add_argument("--workspace", required=True)
    parser.add_argument("--baseline-image")
    parser.add_argument("--candidate-image")
    parser.add_argument("--initializer")
    parser.add_argument("--repetitions", type=int, default=3)
    args = parser.parse_args()
    workspace = Path(args.workspace).resolve()
    if not workspace.is_relative_to(ROOT / ".artifacts") or workspace == ROOT / ".artifacts":
        raise RuntimeError("Workspace must be a child of the repository .artifacts directory")
    if not 1 <= args.repetitions <= 10:
        raise RuntimeError("Repetitions must be between 1 and 10")
    require_local_engine()
    workspace.mkdir(parents=True, exist_ok=True)
    if args.phase == "prepare":
        if not args.baseline_image or not args.initializer:
            raise RuntimeError("Prepare requires baseline image and built initializer DLL")
        prepare(args, workspace)
    else:
        state = json.loads((workspace / "private.json").read_text(encoding="utf-8"))
        if not re.fullmatch("[a-f0-9]{12}", state["runId"]) or state["fixtureDb"] != "r2r_fixture_" + state["runId"]:
            raise RuntimeError("Invalid benchmark ownership manifest")
        if args.phase == "cleanup":
            cleanup(workspace, state)
        else:
            if not args.baseline_image or not args.candidate_image:
                raise RuntimeError("Measure requires both image variants")
            measure(args, workspace, state)


if __name__ == "__main__":
    main()
