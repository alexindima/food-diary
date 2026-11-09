"""Production regressions and collector boundary tests; no live services required."""
import importlib.util
import contextlib
import io
import json
import pathlib
import re
import tempfile
import unittest
from unittest.mock import patch

import host_collector as collector
import external_monitor as external

ROOT = pathlib.Path(__file__).resolve().parent


class FrontendPipelineTests(unittest.TestCase):
    def test_actual_emitter_format_is_classified(self):
        config = (ROOT / "promtail-config.yaml").read_text(encoding="utf-8")
        pattern = re.search(r"expression: '(.+Client telemetry event.+)'", config).group(1)
        for category, name in [("client_error", "global-error"), ("http_request", "api.request"),
                               ("route_timing", "router.navigation"), ("web_vital", "lcp")]:
            with self.subTest(category=category):
                line = f"      Client telemetry event {category}/{name}. Outcome=success; BuildVersion=fixture"
                match = re.search(pattern, line)
                self.assertIsNotNone(match, "deployed telemetry format must retain its category")
                self.assertEqual(category, match.group("client_telemetry_category"))
                self.assertEqual(name, match.group("client_telemetry_name"))


class HostCollectorTests(unittest.TestCase):
    def test_rabbit_probe_allows_healthy_cli_startup_before_rpc(self):
        def delayed_cli(args, **options):
            # The broker is responsive, but starting its separate Erlang CLI VM
            # can exceed ten seconds on the shared VPS during deployment.
            if options["timeout"] < 12:
                raise collector.subprocess.TimeoutExpired(args, options["timeout"])
            return collector.subprocess.CompletedProcess(args, 0,
                '[{"messages_ready":3,"messages_unacknowledged":2,"consumers":1}]')

        metrics = collector.Metrics()
        with patch.object(collector.subprocess, "run", side_effect=delayed_cli):
            collector.collect_rabbitmq(metrics)
        self.assertIn('fooddiary_rabbitmq_messages_ready 3', metrics.render())
        self.assertIn('fooddiary_rabbitmq_consumers 1', metrics.render())

    def test_failed_probe_records_type_without_command_or_secret(self):
        def fail(_):
            raise collector.subprocess.TimeoutExpired(["private-command", "sensitive-value"], 10, output="private-response")

        errors = io.StringIO()
        with patch.object(collector, "collect_containers"), patch.object(collector, "collect_container_resources", create=True), patch.object(collector, "collect_redis"), patch.object(
            collector, "collect_public"
        ), patch.object(collector, "collect_rabbitmq", side_effect=fail), patch.object(
            collector, "DATABASES", {}
        ), patch.object(collector, "BACKUPS", []), contextlib.redirect_stderr(errors):
            text = collector.collect(pathlib.Path("unused"))
        self.assertIn('component="rabbitmq"} 0', text)
        self.assertIn('component="redis"} 1', text)
        self.assertEqual("FoodDiary metric probe failed: component=rabbitmq; error=TimeoutExpired\n", errors.getvalue())

    def test_multiple_databases_emit_one_type_declaration(self):
        metrics = collector.Metrics()
        for database in ["main", "mailrelay", "mailinbox"]:
            partial = collector.Metrics()
            partial.add("postgres_connections", 1, database=database)
            metrics.merge(partial)
        self.assertEqual(1, metrics.render().count("# TYPE fooddiary_postgres_connections gauge"))
        self.assertEqual(3, metrics.render().count("fooddiary_postgres_connections{"))

    def test_container_working_set_excludes_reclaimable_cache(self):
        info = {"State": {"Running": True, "StartedAt": "2026-11-07T00:00:00.123456789Z", "Health": {"Status": "unhealthy"}}, "RestartCount": 2}
        stats = {"memory_stats": {"usage": 1000, "limit": 2000, "stats": {"inactive_file": 300}},
                 "cpu_stats": {"cpu_usage": {"total_usage": 2500000000}}}
        observed = collector.container_resources(info, stats)
        self.assertEqual(700, observed["memory_working_set_bytes"])
        self.assertEqual(2.5, observed["cpu_usage_seconds_total"])

    def test_newest_corrupt_backup_does_not_fall_back_to_old_success(self):
        with tempfile.TemporaryDirectory() as folder:
            root = pathlib.Path(folder)
            for name in ["20261106T024500Z", "20261107T024500Z"]:
                directory = root / name
                directory.mkdir()
                (directory / "fooddiary.dump").write_bytes(b"fixture")
                digest = collector.hashlib.sha256(b"fixture").hexdigest()
                (directory / "SHA256SUMS").write_text(digest + "  fooddiary.dump\n")
            (directory / "fooddiary.dump").write_bytes(b"corrupted")
            observed = collector.backup_observation(root, "fooddiary")
            self.assertEqual(0, observed["verified"])
            self.assertEqual(0, observed["last_success_timestamp_seconds"])

    def test_atomic_file_replaces_old_content_without_partial_samples(self):
        with tempfile.TemporaryDirectory() as folder:
            path = pathlib.Path(folder) / "test.prom"
            path.write_text("old\n")
            collector.atomic_write(path, "new 1\n")
            self.assertEqual("new 1\n", path.read_text())
            self.assertEqual([path], list(path.parent.iterdir()))

    def test_rejects_nonfinite_observations(self):
        for number in [float("nan"), float("inf"), -1]:
            with self.assertRaises(ValueError):
                collector.Metrics().add("fixture", number)

    def test_redis_parser_preserves_only_info_fields_without_key_reads(self):
        self.assertEqual({"used_memory": "123", "keyspace_hits": "5"},
                         collector.parse_redis("# Memory\r\nused_memory:123\r\n\r\nkeyspace_hits:5\r\n"))


class ContainerAvailabilityTests(unittest.TestCase):
    def inventory(self, *services):
        return [{"Id": service, "State": "running", "Labels": {
            "com.docker.compose.project": "fooddiary", "com.docker.compose.service": service,
        }} for service in services]

    def info(self):
        return {"State": {"Running": True, "StartedAt": "2026-11-08T14:59:34.123456789Z",
                          "Health": {"Status": "healthy"}}, "RestartCount": 0}

    def collect(self, docker):
        with patch.object(collector, "docker_get", side_effect=docker), patch.object(
            collector, "collect_redis"
        ), patch.object(collector, "collect_rabbitmq"), patch.object(
            collector, "collect_public"
        ), patch.object(collector, "DATABASES", {}), patch.object(collector, "BACKUPS", []):
            return collector.collect(pathlib.Path("unused"))

    def test_resource_timeout_preserves_running_and_health_observations(self):
        def docker(path):
            if path == "/containers/json?all=true":
                return self.inventory("api-green", "job-manager", "telegram-bot")
            if "/stats?" in path:
                raise TimeoutError("private Docker response")
            return self.info()

        errors = io.StringIO()
        with contextlib.redirect_stderr(errors):
            text = self.collect(docker)
        for service in ["api", "job-manager", "telegram-bot"]:
            self.assertIn(f'fooddiary_container_running{{service="{service}"}} 1', text,
                          "resource timeout must not erase container availability")
            self.assertIn(f'fooddiary_container_healthy{{service="{service}"}} 1', text)
        self.assertIn('fooddiary_collector_component_success{component="containers"} 1', text)
        self.assertIn('fooddiary_collector_component_success{component="container_resources"} 0', text)
        self.assertNotIn('fooddiary_container_memory_working_set_bytes', text)
        self.assertNotIn("private Docker response", errors.getvalue())

    def test_inventory_failure_emits_telemetry_failure_without_fabricated_downtime(self):
        errors = io.StringIO()
        with contextlib.redirect_stderr(errors):
            text = self.collect(lambda _: (_ for _ in ()).throw(TimeoutError()))
        self.assertIn('fooddiary_collector_component_success{component="containers"} 0', text)
        self.assertNotIn('fooddiary_container_running{', text)

    def test_successful_inventory_still_reports_a_missing_required_service(self):
        metrics = collector.Metrics()
        with patch.object(collector, "docker_get", return_value=[]):
            collector.collect_containers(metrics)
        self.assertIn('fooddiary_container_running{service="api"} 0', metrics.render())

    def test_unhealthy_container_state_keeps_restart_and_oom_evidence_without_stats(self):
        info = self.info()
        info["State"]["Health"]["Status"] = "unhealthy"
        info["State"]["OOMKilled"] = True
        info["RestartCount"] = 2
        observed = collector.container_state(info)
        self.assertEqual(1, observed["running"])
        self.assertEqual(0, observed["healthy"])
        self.assertEqual(2, observed["restarts"])
        self.assertEqual(1, observed["oom_killed"])

    def test_blue_green_generations_keep_one_stable_service_identity(self):
        def docker(path):
            if path == "/containers/json?all=true":
                return self.inventory("api", "api-green")
            if "/stats?" in path:
                return {"memory_stats": {"usage": 100}, "cpu_stats": {"cpu_usage": {"total_usage": 1}}}
            return self.info()

        metrics = collector.Metrics()
        with patch.object(collector, "docker_get", side_effect=docker):
            collector.collect_containers(metrics)
        self.assertIn('fooddiary_container_running{service="api"} 2', metrics.render())
        self.assertNotIn('service="api-green"', metrics.render())


class ExternalMonitorTests(unittest.TestCase):
    def observation(self, success, days=30):
        return [{"target": "api", "success": success, "tls_days_remaining": days}]

    def test_two_failures_page_and_a_measured_success_recovers(self):
        first, changes = external.transition({}, self.observation(False, 0), 1)
        self.assertEqual([], changes)
        second, changes = external.transition(first, self.observation(False, 0), 2)
        self.assertEqual([("api", True)], changes)
        _, changes = external.transition(second, self.observation(True), 3)
        self.assertEqual([("api", False)], changes)

    def test_expiring_tls_is_actionable_without_waiting_for_an_outage(self):
        _, changes = external.transition({}, self.observation(True, 10), 1)
        self.assertEqual([("api", True)], changes)

    def test_repeated_failure_does_not_spam_notifications(self):
        state = {"targets": {"api": {"failures": 4, "alerting": True}}}
        _, changes = external.transition(state, self.observation(False, 0), 5)
        self.assertEqual([], changes)


class AlertDimensionTests(unittest.TestCase):
    def test_missing_telemetry_cannot_start_the_container_outage_timer(self):
        import yaml
        rules = yaml.safe_load((ROOT / "grafana/fooddiary-operational-alerting.yml").read_text(encoding="utf-8"))["groups"][0]["rules"]
        rules = {rule["uid"]: rule for rule in rules}
        missing = rules["fd-container-missing"]
        self.assertEqual("OK", missing["noDataState"],
                         "missing telemetry must reset rather than advance the container outage timer")
        self.assertEqual("3m", missing["for"], "prolonged measured downtime must still alert")
        self.assertEqual("1-fooddiary_container_running", missing["data"][0]["model"]["expr"])
        for uid in ["fd-host-probe-failed", "fd-host-probes-stale"]:
            self.assertEqual("critical", rules[uid]["labels"]["severity"])
            self.assertEqual("Alerting", rules[uid]["noDataState"])

    def test_container_alerts_keep_distinct_service_instances(self):
        import yaml
        rules = yaml.safe_load((ROOT / "grafana/fooddiary-operational-alerting.yml").read_text(encoding="utf-8"))["groups"][0]["rules"]
        for rule in rules:
            if rule["uid"] not in {"fd-container-missing", "fd-container-unhealthy", "fd-container-restarted", "fd-container-oom"}:
                continue
            with self.subTest(rule=rule["uid"]):
                identities = {json.dumps({"service": service, **rule["labels"]}, sort_keys=True) for service in ["api", "job-manager"]}
                self.assertEqual(2, len(identities), "alert rule labels must preserve each observed service")


if __name__ == "__main__":
    unittest.main()
