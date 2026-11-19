"""File descriptor telemetry boundaries; synthetic procfs, no live services."""
import contextlib
import io
import json
import os
import pathlib
import sys
import tempfile
import unittest
from unittest.mock import patch

import host_collector as collector


class FileDescriptorTests(unittest.TestCase):
    def setUp(self):
        self.directory = tempfile.TemporaryDirectory()
        self.addCleanup(self.directory.cleanup)
        self.root = pathlib.Path(self.directory.name)
        self.pid = 123
        self.process = self.root / str(self.pid)
        (self.process / "fd").mkdir(parents=True)
        for number in range(7):
            (self.process / "fd" / str(number)).touch()
        self.stat_bytes = b"123 (fixture (process) \xff) S " + b" ".join([b"0"] * 18 + [b"42"]) + b"\n"
        (self.process / "stat").write_bytes(self.stat_bytes)
        (self.process / "limits").write_text("Limit                     Soft Limit           Hard Limit           Units\n"
                                                "Max open files            1024                 65536                files\n")

    def info(self, service="api-green", pid=123, started="2026-11-19T00:00:00.000000001Z"):
        return {"State": {"Running": True, "Pid": pid, "StartedAt": started}, "Config": {"Labels": {
            "com.docker.compose.service": service,
        }}}

    def collect(self, infos=None):
        infos = infos or {"green": self.info()}
        with patch.object(collector, "PROC_ROOT", self.root), patch.object(
            collector, "running_containers", return_value=[("api", key) for key in infos]
        ), patch.object(collector, "docker_get", side_effect=lambda path: infos[path.split("/")[2]]), patch.object(
            collector, "collect_containers"
        ), patch.object(collector, "collect_container_resources"), patch.object(collector, "collect_redis"), patch.object(
            collector, "collect_rabbitmq"
        ), patch.object(collector, "collect_public"), patch.object(collector, "DATABASES", {}), patch.object(collector, "BACKUPS", []):
            return collector.collect(self.root)

    def test_full_collection_exports_count_and_effective_soft_limit(self):
        text = self.collect()
        self.assertIn('fooddiary_container_process_open_fds{service="api",slot="green"} 7', text)
        self.assertIn('fooddiary_container_process_max_fds{service="api",slot="green"} 1024', text)
        self.assertNotIn("65536", text, "the hard limit is not the enforced soft limit")
        self.assertIn('component="container_file_descriptors"} 1', text)
        self.assertNotIn(self.directory.name, text)

    def test_blue_green_process_limits_are_not_added_together(self):
        text = self.collect({"blue": self.info("api"), "green": self.info()})
        for slot in ["blue", "green"]:
            self.assertIn(f'fooddiary_container_process_max_fds{{service="api",slot="{slot}"}} 1024', text)
        self.assertNotIn("2048", text)

    def test_unlimited_limit_does_not_become_a_false_zero(self):
        (self.process / "limits").write_text("Max open files            unlimited            unlimited            files\n")
        text = self.collect()
        self.assertIn('fooddiary_container_process_open_fds{service="api",slot="green"} 7', text)
        self.assertNotIn("fooddiary_container_process_max_fds", text)
        self.assertIn('component="container_file_descriptors"} 1', text)

    def test_disappearing_process_omits_measurements_and_reports_failed_probe(self):
        (self.process / "limits").unlink()
        errors = io.StringIO()
        with contextlib.redirect_stderr(errors):
            text = self.collect()
        self.assertNotIn("fooddiary_container_process_open_fds", text)
        self.assertIn('component="container_file_descriptors"} 0', text)
        self.assertNotIn(self.directory.name, errors.getvalue())

    def test_pid_reuse_during_observation_is_rejected(self):
        original = pathlib.Path.read_bytes
        reads = 0

        def read(path, *args, **kwargs):
            nonlocal reads
            if path.name == "stat":
                reads += 1
                return self.stat_bytes if reads == 1 else self.stat_bytes.replace(b"42", b"43")
            return original(path, *args, **kwargs)

        errors = io.StringIO()
        with patch.object(pathlib.Path, "read_bytes", read), contextlib.redirect_stderr(errors):
            text = self.collect()
        self.assertNotIn("fooddiary_container_process_open_fds", text)
        self.assertIn('component="container_file_descriptors"} 0', text)

    def test_malformed_limits_are_not_reported_as_healthy(self):
        (self.process / "limits").write_text("unexpected private response\n")
        errors = io.StringIO()
        with contextlib.redirect_stderr(errors):
            text = self.collect()
        self.assertNotIn("fooddiary_container_process_open_fds", text)
        self.assertIn('component="container_file_descriptors"} 0', text)
        self.assertNotIn("private response", errors.getvalue())

    def test_incomplete_proc_stat_is_a_probe_failure_not_a_collector_crash(self):
        (self.process / "stat").write_text("123 (fixture) S\n")
        errors = io.StringIO()
        with contextlib.redirect_stderr(errors):
            text = self.collect()
        self.assertNotIn("fooddiary_container_process_open_fds", text)
        self.assertIn('component="container_file_descriptors"} 0', text)

    def test_container_restart_discards_samples_from_the_old_process(self):
        for replacement in [self.info(pid=456), self.info(started="2026-11-19T00:00:00.000000002Z")]:
            with self.subTest(state=replacement["State"]):
                metrics = collector.Metrics()
                with patch.object(collector, "PROC_ROOT", self.root), patch.object(
                    collector, "docker_get", side_effect=[self.info(), replacement]
                ):
                    with self.assertRaisesRegex(RuntimeError, "restarted"):
                        collector.collect_container_file_descriptors(metrics, [("api", "green")])
                self.assertEqual("\n", metrics.render())

    def test_duplicate_slots_do_not_publish_ambiguous_series(self):
        metrics = collector.Metrics()
        with patch.object(collector, "PROC_ROOT", self.root), patch.object(
            collector, "docker_get", return_value=self.info()
        ):
            with self.assertRaisesRegex(ValueError, "Duplicate"):
                collector.collect_container_file_descriptors(metrics, [("api", "first"), ("api", "second")])
        self.assertEqual("\n", metrics.render())

    @unittest.skipUnless(sys.platform == "linux", "requires Linux procfs")
    def test_real_linux_descriptors_and_soft_limit(self):
        import resource
        info = {"State": {"Running": True, "Pid": os.getpid()}}
        before = collector.process_file_descriptors(info)
        with contextlib.ExitStack() as files:
            for _ in range(4):
                files.enter_context(tempfile.TemporaryFile())
            during = collector.process_file_descriptors(info)
        self.assertGreaterEqual(during["open_fds"], before["open_fds"] + 4)
        soft_limit, _ = resource.getrlimit(resource.RLIMIT_NOFILE)
        self.assertEqual(None if soft_limit == resource.RLIM_INFINITY else soft_limit, during.get("max_fds"))


class FileDescriptorPresentationTests(unittest.TestCase):
    def test_threshold_alerts_preserve_each_service_and_slot(self):
        import yaml
        root = pathlib.Path(__file__).resolve().parent
        rules = yaml.safe_load((root / "grafana/fooddiary-operational-alerting.yml").read_text(encoding="utf-8"))["groups"][0]["rules"]
        rules = {rule["uid"]: rule for rule in rules}
        for uid, threshold, severity, pending in [
            ("fd-process-fds-warning", .8, "warning", "2m"),
            ("fd-process-fds-critical", .9, "critical", "0m"),
        ]:
            rule = rules[uid]
            self.assertEqual(threshold, rule["data"][2]["model"]["conditions"][0]["evaluator"]["params"][0])
            self.assertEqual(severity, rule["labels"]["severity"])
            self.assertEqual(pending, rule["for"])
            self.assertNotIn("service", rule["labels"])
            self.assertIn("slot", rule["annotations"]["summary"])
            self.assertEqual("OK", rule["noDataState"], "probe failure must not be labelled descriptor exhaustion")
            self.assertNotIn("vector(0)", rule["data"][0]["model"]["expr"])

    def test_dashboard_exposes_usage_limit_and_ratio_without_aggregation(self):
        root = pathlib.Path(__file__).resolve().parent
        dashboard = json.loads((root / "grafana/fooddiary-operations.json").read_text(encoding="utf-8"))
        panels = {panel["title"]: panel for panel in dashboard["panels"]}
        counts = panels["Process open file descriptors / soft limit"]
        self.assertEqual(2, len(counts["targets"]))
        ratio = panels["Process file descriptor limit used"]
        self.assertEqual("percentunit", ratio["fieldConfig"]["defaults"]["unit"])
        self.assertNotIn("sum", ratio["targets"][0]["expr"])
        self.assertIn("primary", ratio["description"])


if __name__ == "__main__":
    unittest.main()
