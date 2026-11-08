#!/usr/bin/env python3
"""Install the reviewed observability assets on fooddiary-prod.

Default is validation only. --apply backs up each owned target under a private
directory. Application containers, databases, credentials and trace pipelines
are not changed. Run from an uploaded copy of this directory, as root.
"""
import argparse
import datetime as dt
import json
import os
import pathlib
import shutil
import stat
import subprocess
import tempfile
import time
import urllib.parse
import urllib.request

import yaml

ROOT = pathlib.Path(__file__).resolve().parent
PROVISION = pathlib.Path("/etc/grafana/provisioning/alerting")
PROM_CONFIG = pathlib.Path("/opt/prometheus/prometheus.yml")
PROMTAIL_CONFIG = pathlib.Path("/opt/promtail/promtail-config.yaml")


def command(args):
    result = subprocess.run(args, capture_output=True, timeout=35)
    if result.returncode:
        # Provisioned configuration can contain credentials. Do not echo output.
        raise RuntimeError("Validation/service command failed: " + pathlib.Path(args[0]).name)


def validate_metrics(path):
    with pathlib.Path(path).open("rb") as stream:
        result = subprocess.run(["/opt/prometheus/promtool", "check", "metrics"], stdin=stream, capture_output=True, timeout=10)
    if result.returncode:
        raise RuntimeError("Numeric metric exposition validation failed")


def get_json(url):
    with urllib.request.urlopen(url, timeout=5) as response:
        return json.load(response)


def prepare_prometheus():
    config = yaml.safe_load(PROM_CONFIG.read_text())
    rule_files = config.setdefault("rule_files", [])
    target = "/opt/fooddiary-observability/prometheus/fooddiary-recording.yml"
    if target not in rule_files:
        rule_files.append(target)
    existing = {job["job_name"] for job in config["scrape_configs"]}
    available = []
    for name, port in [("prometheus", 9090), ("loki", 3100), ("promtail", 9080), ("otel-internal", 8888)]:
        try:
            with urllib.request.urlopen(f"http://127.0.0.1:{port}/metrics", timeout=3) as response:
                if response.status == 200:
                    available.append(name)
        except OSError:
            continue
        if name not in existing:
            config["scrape_configs"].append({"job_name": name, "static_configs": [{"targets": [f"127.0.0.1:{port}"]}]})
    return config, available


def validate():
    command(["/opt/prometheus/promtool", "check", "rules", str(ROOT / "prometheus/fooddiary-recording.yml")])
    command(["/opt/promtail/promtail-linux-amd64", "-config.file=" + str(ROOT / "promtail-config.yaml"), "-check-syntax"])
    rules = yaml.safe_load((ROOT / "grafana/fooddiary-operational-alerting.yml").read_text())
    for group in rules["groups"]:
        for rule in group["rules"]:
            model = rule["data"][0]["model"]
            expr = model["expr"]
            endpoint = "http://127.0.0.1:3100/loki/api/v1/query?" if model["datasource"]["type"] == "loki" else "http://127.0.0.1:9090/api/v1/query?"
            result = get_json(endpoint + urllib.parse.urlencode({"query": expr}))
            if result["status"] != "success":
                raise RuntimeError("Invalid operational expression: " + rule["uid"])
    for path in (ROOT / "grafana").glob("*.json"):
        dashboard = json.loads(path.read_text())
        if not dashboard.get("uid") or not isinstance(dashboard.get("panels"), list):
            raise RuntimeError("Invalid dashboard structure")


def apply(config):
    stamp = dt.datetime.now(dt.timezone.utc).strftime("%Y%m%dT%H%M%SZ")
    backup = pathlib.Path("/var/backups/fooddiary-observability") / stamp
    backup.mkdir(parents=True, mode=0o700)
    os.chmod(backup.parent, 0o700)
    manifest = []
    def install(source, target, mode=0o644):
        target = pathlib.Path(target)
        index = len(manifest)
        entry = {"target": str(target), "backup": str(index), "existed": target.exists()}
        target_mode = stat.S_IMODE(target.stat().st_mode) if target.exists() else mode
        if target.exists():
            shutil.copy2(target, backup / str(index))
        manifest.append(entry)
        (backup / "manifest.json").write_text(json.dumps(manifest, indent=2))
        target.parent.mkdir(parents=True, exist_ok=True)
        shutil.copyfile(source, target)
        os.chmod(target, target_mode)
    for filename in ["host_collector.py", "external_monitor.py"]:
        install(ROOT / filename, "/opt/fooddiary-observability/" + filename)
    install(ROOT / "prometheus/fooddiary-recording.yml", "/opt/fooddiary-observability/prometheus/fooddiary-recording.yml")
    for path in (ROOT / "grafana").glob("*.json"):
        install(path, "/var/lib/grafana/dashboards/" + path.name)
    for name in ["fooddiary-backend-alerting.yml", "fooddiary-operational-alerting.yml"]:
        install(ROOT / "grafana" / name, PROVISION / name)
    install(ROOT / "promtail-config.yaml", PROMTAIL_CONFIG)
    for name in ["fooddiary-metrics.service", "fooddiary-metrics.timer"]:
        install(ROOT / "systemd" / name, "/etc/systemd/system/" + name)
    with tempfile.TemporaryDirectory(prefix="fooddiary-observability-") as directory:
        temporary = pathlib.Path(directory)
        prometheus = temporary / "prometheus.yml"
        prometheus.write_text(yaml.safe_dump(config, sort_keys=False))
        command(["/opt/prometheus/promtool", "check", "config", str(prometheus)])
        install(prometheus, PROM_CONFIG)
        dropin = temporary / "textfile.conf"
        dropin.write_text("[Service]\nExecStart=\nExecStart=/usr/bin/prometheus-node-exporter --web.listen-address=127.0.0.1:9100 --collector.textfile.directory=/var/lib/prometheus/node-exporter\n")
        install(dropin, "/etc/systemd/system/prometheus-node-exporter.service.d/fooddiary-textfile.conf")
    pathlib.Path("/var/lib/prometheus/node-exporter").mkdir(parents=True, exist_ok=True)
    command(["/usr/bin/python3", "/opt/fooddiary-observability/host_collector.py"])
    validate_metrics("/var/lib/prometheus/node-exporter/fooddiary.prom")
    command(["systemctl", "daemon-reload"])
    command(["systemctl", "restart", "prometheus-node-exporter"])
    command(["systemctl", "kill", "--signal=HUP", "prometheus"])
    command(["systemctl", "restart", "promtail"])
    command(["systemctl", "restart", "grafana-server"])
    command(["systemctl", "enable", "--now", "fooddiary-metrics.timer"])
    for attempt in range(25):
        try:
            if get_json("http://127.0.0.1:3000/api/health").get("database") == "ok":
                break
        except OSError:
            time.sleep(1)
    else:
        raise RuntimeError("Grafana did not recover; restore the private manifest")
    print(json.dumps({"applied": True, "backup": str(backup), "owned_targets": len(manifest)}))


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--apply", action="store_true")
    args = parser.parse_args()
    if os.geteuid() != 0:
        raise RuntimeError("Run on fooddiary-prod as root")
    validate()
    config, available = prepare_prometheus()
    print(json.dumps({"validated": True, "available_monitoring_targets": available, "trace_pipeline_changed": False}))
    if args.apply:
        apply(config)


if __name__ == "__main__":
    main()
