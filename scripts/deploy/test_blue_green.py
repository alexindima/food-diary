"""Exercise release decisions and handover against owned Docker/HTTP boundaries."""

from collections import deque
from http.server import BaseHTTPRequestHandler, ThreadingHTTPServer
import json
from pathlib import Path
import subprocess
import tempfile
import threading
import unittest
from unittest.mock import patch

import blue_green as deployment


SHA = "a" * 40
OLD_SHA = "b" * 40
CONTRACT_SHA = "c" * 40
BASELINE = "20261017130323_InitialBaseline"
NEXT = "20261102000000_TestMigration"
ROOT = Path(__file__).resolve().parents[2]


def configuration():
    return {"services": {
        "api": {"ports": [{"target": 5000, "published": "5000", "host_ip": "127.0.0.1"}]},
        "api-green": {"ports": [{"target": 5000, "published": "5001", "host_ip": "127.0.0.1"}]},
        "client-ssr": {"ports": [{"target": 4000, "published": "4000", "host_ip": "127.0.0.1"}],
                       "environment": {"SSR_API_ORIGIN": "http://api:5000"}},
        "client-ssr-green": {"ports": [{"target": 4000, "published": "4001", "host_ip": "127.0.0.1"}],
                             "environment": {"SSR_API_ORIGIN": "http://api-green:5000"}},
        "telegram-bot": {"environment": {}}, "nginx": {"image": "nginx@sha256:" + "c" * 64},
    }}


class Host(deployment.Deployment):
    def __init__(self, root):
        super().__init__(root, {"DEPLOY_COMMIT_SHA": SHA, "DEPLOY_IMAGE_TAG": "sha-" + SHA, "DEPLOY_API_CONTRACT_SHA": CONTRACT_SHA})
        self.events = []
        self.config = configuration()
        self.services = {"api", "client-ssr", "telegram-bot", "job-manager", "nginx"}
        self.versions = {5000: OLD_SHA}
        self.applied = [BASELINE]
        self.inventory = "applied  " + BASELINE + "\n"
        self.bad_version = False
        self.ssr_failed = False
        self.no_memory = False
        self.nginx_mismatch = False
        self.nginx_failure = False
        self.proxy_failure = False
        self.active_port = 5000
        self.worker_snapshots = deque()
        deployment.write_atomic(self.state_path, json.dumps({"active": "blue", "sha": OLD_SHA, "apiContractSha": CONTRACT_SHA}))
        deployment.write_atomic(self.route_path, deployment.routes("blue"), 0o644)

    def run(self, *arguments, input=None, required=True):
        self.events.append((arguments, input))
        output = ""
        if arguments[:2] == ("docker", "compose"):
            if "config" in arguments:
                output = json.dumps(self.config)
            elif "ps" in arguments:
                output = "id-" + arguments[-1] if arguments[-1] in self.services else ""
            elif "list" in arguments:
                output = self.inventory
            elif "postgres" in arguments:
                self.assert_readonly = "default_transaction_read_only=on" in " ".join(arguments)
                output = json.dumps(self.applied)
            elif "up" in arguments:
                service = arguments[-1]
                self.services.add(service)
                if service in ("api", "api-green"):
                    self.versions[5000 if service == "api" else 5001] = OLD_SHA if self.bad_version else SHA
            elif "stop" in arguments or "start" in arguments:
                action = "stop" if "stop" in arguments else "start"
                for service in arguments[arguments.index(action) + 1:]:
                    if action == "stop":
                        self.services.discard(service)
                    else:
                        self.services.add(service)
            elif arguments[-1] == "-t" and self.nginx_failure:
                self.nginx_failure = False
                raise deployment.DeploymentError("Nginx config rejected")
            elif arguments[-1] == "reload":
                port = int(self.route_path.read_text().split("upstream fooddiary_api { server 127.0.0.1:")[1].split(";")[0])
                if self.proxy_failure and port == 5001:
                    self.proxy_failure = False
                    raise deployment.DeploymentError("Nginx reload failed")
                self.active_port = port
        elif arguments[:2] == ("docker", "top"):
            snapshot = self.worker_snapshots.popleft() if self.worker_snapshots else []
            self.events.append(("workers", snapshot))
            output = "PID COMMAND\n" + "\n".join(f"{pid} nginx: worker process" for pid in snapshot)
        elif arguments[:2] == ("docker", "inspect"):
            output = "nginx-image"
        elif arguments[:3] == ("docker", "image", "inspect"):
            output = "other-image" if self.nginx_mismatch else "nginx-image"
        else:
            raise AssertionError(f"Unexpected external command: {arguments}")
        return subprocess.CompletedProcess(arguments, 0, output, "")

    def request(self, url, data=None, headers=None):
        self.events.append(("http", url))
        port = int(url.split(":")[2].split("/")[0])
        port = self.active_port if port == deployment.PROBE_PORT else port
        if url.endswith("/internal/version"):
            sha = self.versions.get(port, "")
            return 200, json.dumps({"commitSha": sha, "imageTag": "sha-" + sha})
        if url.endswith("/health/ready"):
            return 200, "Healthy" if port in self.versions else "Unhealthy"
        return (503, "unavailable") if self.ssr_failed else (200, "SSR fixture")

    def memory_headroom(self):
        self.events.append(("memory",))
        if self.no_memory:
            raise deployment.DeploymentError("Insufficient memory headroom")

    def wait(self, predicate, seconds, message):
        if not predicate():
            raise deployment.DeploymentError(message)


class BlueGreenTests(unittest.TestCase):
    def setUp(self):
        temporary = tempfile.TemporaryDirectory(prefix="fd-release-")
        self.addCleanup(temporary.cleanup)
        self.host = Host(temporary.name)

    def commands(self, action):
        return [event[0] for event in self.host.events if isinstance(event[0], tuple) and action in event[0]]

    def ready_candidate(self):
        self.assertEqual("overlap", self.host.plan())
        self.host.start("api")
        self.host.start("ssr")

    def test_no_migration_handover_keeps_old_until_ready_and_drained(self):
        self.ready_candidate()
        self.assertIn("api", self.host.services)
        self.assertIn("client-ssr", self.host.services)
        self.assertFalse(self.commands("stop"))
        self.assertEqual(OLD_SHA, json.loads(self.host.state_path.read_text())["sha"])
        self.assertEqual(5000, self.host.active_port)
        self.assertTrue(self.host.assert_readonly)
        self.host.worker_snapshots = deque([[10], [10, 11], [10], [11], []])
        self.host.switch()
        self.assertEqual(5001, self.host.active_port)
        self.assertEqual(OLD_SHA, json.loads(self.host.state_path.read_text())["sha"])
        with patch.object(deployment.time, "sleep"):
            self.host.finish()
        self.assertEqual("green", json.loads(self.host.state_path.read_text())["active"])
        self.assertNotIn("api", self.host.services)
        self.assertNotIn("client-ssr", self.host.services)
        self.assertIn("api-green", self.host.services)
        self.assertIn("telegram-bot", self.host.services)
        self.assertFalse(any("update" in command for command in self.commands("run")))
        self.assertEqual(1, len(self.commands("stop")))
        self.assertEqual(("api", "client-ssr"), self.commands("stop")[0][-2:])
        stop_index = next(index for index, event in enumerate(self.host.events) if isinstance(event[0], tuple) and "stop" in event[0])
        last_drain = max(index for index, event in enumerate(self.host.events) if event == ("workers", []))
        self.assertLess(last_drain, stop_index)

    def test_next_release_alternates_to_blue(self):
        self.ready_candidate()
        self.host.switch()
        with patch.object(deployment.time, "sleep"):
            self.host.finish()
        self.assertEqual("overlap", self.host.plan())
        self.assertEqual("blue", self.host.load()["candidate"])

    def test_interrupted_retirement_rechecks_candidate_and_completes_before_replanning(self):
        self.ready_candidate()
        self.host.switch()
        compose = self.host.compose
        def fail_retirement(*arguments, **kwargs):
            if arguments[0] == "stop":
                raise deployment.DeploymentError("Interrupted retirement")
            return compose(*arguments, **kwargs)
        with patch.object(self.host, "compose", side_effect=fail_retirement), patch.object(deployment.time, "sleep"):
            with self.assertRaisesRegex(deployment.DeploymentError, "Interrupted retirement"):
                self.host.finish()
        self.assertEqual("retiring", self.host.load()["phase"])
        self.assertEqual("overlap", self.host.plan())
        self.assertEqual("green", json.loads(self.host.state_path.read_text())["active"])
        self.assertEqual("blue", self.host.load()["candidate"])

    def test_drain_deadline_is_bounded_for_persistent_connections(self):
        self.ready_candidate()
        plan = self.host.load()
        plan["workers"] = [10]
        self.host.worker_snapshots = deque([[10], [10]])
        with patch.object(deployment.time, "monotonic", side_effect=[0, deployment.DRAIN_SECONDS + 1]), patch("builtins.print") as output:
            self.host.drain(plan)
        self.assertIn("Bounded", output.call_args.args[0])

    def test_candidate_wrong_revision_does_not_change_active_traffic(self):
        self.host.plan()
        self.host.bad_version = True
        with self.assertRaisesRegex(deployment.DeploymentError, "revision"):
            self.host.start("api")
        self.host.abort()
        self.assertEqual(5000, self.host.active_port)
        self.assertIn("api", self.host.services)
        self.assertNotIn("api-green", self.host.services)
        self.assertEqual(OLD_SHA, json.loads(self.host.state_path.read_text())["sha"])

    def test_candidate_ssr_failure_keeps_active_pair(self):
        self.host.plan()
        self.host.start("api")
        self.host.ssr_failed = True
        with self.assertRaisesRegex(deployment.DeploymentError, "SSR"):
            self.host.start("ssr")
        self.host.abort()
        self.assertTrue({"api", "client-ssr"} <= self.host.services)
        self.assertFalse({"api-green", "client-ssr-green"} & self.host.services)

    def test_low_memory_and_nginx_image_change_fail_before_candidate_start(self):
        for option in ("no_memory", "nginx_mismatch"):
            with self.subTest(option=option):
                setattr(self.host, option, True)
                with self.assertRaises(deployment.DeploymentError):
                    self.host.plan()
                self.assertFalse(self.commands("up"))
                self.assertFalse(self.commands("stop"))
                self.assertFalse(self.host.plan_path.exists())
                setattr(self.host, option, False)

    def test_case_insensitive_bot_slot_origin_is_rejected_before_startup(self):
        self.host.config["services"]["telegram-bot"]["environment"]["TELEGRAMBOT__APIBASEURL"] = "http://api:5000"
        with self.assertRaisesRegex(deployment.DeploymentError, "stable HTTPS ingress"):
            self.host.plan()
        self.assertFalse(self.commands("up"))

    def test_pending_migration_or_manual_override_uses_maintenance(self):
        self.host.inventory += "pending  " + NEXT + "\n"
        self.assertEqual("maintenance", self.host.plan())
        self.assertEqual("blue", self.host.load()["candidate"])
        self.host.abort()
        self.host.inventory = "applied " + BASELINE + "\n"
        self.assertEqual("maintenance", self.host.plan("maintenance"))

    def test_legacy_bootstrap_never_overlaps_an_unverified_old_api(self):
        self.host.state_path.unlink()
        self.assertEqual("maintenance", self.host.plan())
        self.assertEqual("blue", self.host.load()["candidate"])

    def failed_ready_bootstrap(self):
        self.host.state_path.unlink()
        self.assertEqual("maintenance", self.host.plan())
        self.host.start("api")
        failed = self.host.load()
        self.host.environment.update(DEPLOY_COMMIT_SHA="d" * 40, DEPLOY_IMAGE_TAG="sha-" + "d" * 40)
        return failed

    def test_verified_first_bootstrap_rolls_forward_in_maintenance_and_preserves_failed_plan(self):
        failed = self.failed_ready_bootstrap()
        self.assertEqual("maintenance", self.host.plan())
        retry = self.host.load()
        self.assertEqual(failed, retry["recoveredFrom"])
        self.assertEqual("d" * 40, retry["sha"])
        self.assertEqual("blue", retry["candidate"])
        self.assertFalse(self.commands("start"))
        self.assertFalse(self.commands("stop"))
        self.assertEqual(SHA, self.host.versions[5000])
        self.assertFalse(self.host.state_path.exists())

    def test_first_bootstrap_retry_rejects_incompatible_inputs_without_replacing_plan(self):
        self.failed_ready_bootstrap()
        original = self.host.plan_path.read_bytes()
        cases = (
            (lambda: self.host.environment.update(DEPLOY_API_CONTRACT_SHA="e" * 40),
             lambda: self.host.environment.update(DEPLOY_API_CONTRACT_SHA=CONTRACT_SHA)),
            (lambda: setattr(self.host, "inventory", "applied " + BASELINE + "\npending " + NEXT + "\n"),
             lambda: setattr(self.host, "inventory", "applied " + BASELINE + "\n")),
            (lambda: self.host.versions.update({5000: OLD_SHA}),
             lambda: self.host.versions.update({5000: SHA})),
            (lambda: deployment.write_atomic(self.host.route_path, deployment.routes("green")),
             lambda: deployment.write_atomic(self.host.route_path, deployment.routes("blue"))),
            (lambda: self.host.config["services"]["api-green"]["ports"][0].update(published="5002"),
             lambda: self.host.config["services"]["api-green"]["ports"][0].update(published="5001")),
            (lambda: self.host.services.discard("api"), lambda: self.host.services.add("api")),
        )
        for index, (change, restore) in enumerate(cases):
            with self.subTest(index=index):
                change()
                with self.assertRaisesRegex(deployment.DeploymentError, "Bootstrap forward recovery"):
                    self.host.plan()
                self.assertEqual(original, self.host.plan_path.read_bytes())
                self.assertFalse(self.commands("start"))
                self.assertFalse(self.commands("stop"))
                restore()
        self.host.applied.append(NEXT)
        with self.assertRaisesRegex(deployment.DeploymentError, "unknown"):
            self.host.plan()
        self.assertEqual(original, self.host.plan_path.read_bytes())

    def test_managed_maintenance_failure_after_api_readiness_still_requires_explicit_recovery(self):
        self.host.plan("maintenance")
        self.host.start("api")
        original = self.host.plan_path.read_bytes()
        with self.assertRaisesRegex(deployment.DeploymentError, "migration may have run"):
            self.host.plan()
        self.assertEqual(original, self.host.plan_path.read_bytes())
        self.assertFalse(self.commands("start"))

    def test_changed_or_unknown_api_contract_selects_maintenance(self):
        for contract in (None, "d" * 40):
            with self.subTest(contract=contract):
                deployment.write_atomic(self.host.state_path, json.dumps({"active": "blue", "sha": OLD_SHA, "apiContractSha": contract}))
                self.assertEqual("maintenance", self.host.plan())
                self.host.abort()

    def test_missing_contract_fingerprint_blocks_before_external_commands(self):
        self.host.environment.pop("DEPLOY_API_CONTRACT_SHA")
        with self.assertRaisesRegex(deployment.DeploymentError, "contract tree fingerprint"):
            self.host.plan()
        self.assertEqual([], self.host.events)

    def test_bootstrap_initializes_blue_before_ingress_upload_and_preserves_existing_routes(self):
        self.host.state_path.unlink()
        self.host.route_path.unlink()
        self.host.initialize_routing()
        self.assertEqual(deployment.routes("blue"), self.host.route_path.read_text())
        deployment.write_atomic(self.host.route_path, deployment.routes("green"), 0o644)
        self.host.initialize_routing()
        self.assertEqual(deployment.routes("green"), self.host.route_path.read_text())
        self.assertFalse(self.commands("up"))

    def test_bootstrap_does_not_guess_route_for_existing_managed_state(self):
        self.host.route_path.unlink()
        with self.assertRaisesRegex(deployment.DeploymentError, "Managed routing is missing"):
            self.host.initialize_routing()
        self.assertFalse(self.host.route_path.exists())

    def test_backup_failure_restores_only_previously_running_writers(self):
        self.host.services.discard("telegram-bot")
        self.host.plan("maintenance")
        self.host.services.discard("api")  # Shell stops writers, then backup fails.
        self.host.services.discard("job-manager")
        self.host.abort()
        self.assertEqual(("api", "job-manager"), self.commands("start")[0][-2:])
        self.assertIn("api", self.host.services)
        self.assertNotIn("telegram-bot", self.host.services)

    def test_migration_attempt_never_restarts_old_images(self):
        self.host.plan("maintenance")
        plan = self.host.load()
        plan["phase"] = "migration-started"
        self.host.save(plan)
        with self.assertRaisesRegex(deployment.DeploymentError, "migration may have run"):
            self.host.abort()
        self.assertFalse(self.commands("start"))

    def test_invalid_nginx_config_or_reload_restores_active_route(self):
        for option in ("nginx_failure", "proxy_failure"):
            with self.subTest(option=option):
                self.ready_candidate()
                setattr(self.host, option, True)
                with self.assertRaisesRegex(deployment.DeploymentError, "Nginx"):
                    self.host.switch()
                self.host.abort()
                self.assertEqual(5000, self.host.active_port)
                self.assertEqual(deployment.routes("blue"), self.host.route_path.read_text())
                self.assertIn("api", self.host.services)

    def test_post_switch_abort_and_interrupted_next_deploy_restore_old_slot(self):
        self.ready_candidate()
        self.host.switch()
        self.assertEqual(5001, self.host.active_port)
        # The next planner must recover the uncommitted previous handover first.
        self.assertEqual("overlap", self.host.plan())
        self.assertEqual(5000, self.host.active_port)
        self.assertNotIn("api-green", self.host.services)

    def test_switch_and_finish_require_readiness_and_current_proxy_probes(self):
        self.host.plan()
        with self.assertRaises(deployment.DeploymentError):
            self.host.switch()
        self.host.start("api")
        self.host.start("ssr")
        self.host.switch()
        self.host.versions[5001] = OLD_SHA
        with self.assertRaisesRegex(deployment.DeploymentError, "no longer healthy"):
            self.host.finish()
        self.assertFalse(self.commands("stop"))

    def test_interrupted_switch_retries_only_the_verified_candidate_and_keeps_workers(self):
        self.host.plan("maintenance")
        self.host.start("api")
        self.host.start("ssr")
        self.host.worker_snapshots = deque([[10], [11]])
        with patch.object(self.host, "reload", side_effect=deployment.DeploymentError("Interrupted reload")):
            with self.assertRaisesRegex(deployment.DeploymentError, "Interrupted reload"):
                self.host.switch()
        self.assertEqual("switching", self.host.load()["phase"])
        self.host.switch()
        self.assertEqual([10, 11], self.host.load()["workers"])
        self.host.finish()
        self.assertEqual(SHA, json.loads(self.host.state_path.read_text())["sha"])
        self.assertEqual("complete", self.host.load()["phase"])
        self.assertFalse(self.commands("stop"))

    def test_interrupted_switch_rejects_unready_candidate_without_mutating_plan_or_routes(self):
        self.ready_candidate()
        with patch.object(self.host, "reload", side_effect=deployment.DeploymentError("Interrupted reload")):
            with self.assertRaises(deployment.DeploymentError):
                self.host.switch()
        original = self.host.plan_path.read_bytes(), self.host.route_path.read_bytes()
        for failure in ("api", "ssr", "warmup"):
            with self.subTest(failure=failure):
                request = self.host.request
                def unready(url, data=None, headers=None):
                    if (failure == "api" and url.endswith("/internal/version")
                            or failure == "ssr" and url.endswith("/health/ssr")
                            or failure == "warmup" and url.endswith("/explore")):
                        return 503, "unavailable"
                    return request(url, data, headers)
                with patch.object(self.host, "request", side_effect=unready):
                    with self.assertRaisesRegex(deployment.DeploymentError, "candidate.*ready"):
                        self.host.switch()
                self.assertEqual(original, (self.host.plan_path.read_bytes(), self.host.route_path.read_bytes()))
                self.assertFalse(self.commands("stop"))

    def test_inactive_slot_or_route_state_drift_is_not_overwritten(self):
        self.host.services.add("api-green")
        with self.assertRaisesRegex(deployment.DeploymentError, "unexpectedly running"):
            self.host.plan()
        self.assertFalse(self.commands("up"))
        self.host.services.discard("api-green")
        deployment.write_atomic(self.host.state_path, json.dumps({"active": "blue", "sha": OLD_SHA}))
        self.host.route_path.unlink()
        with self.assertRaisesRegex(deployment.DeploymentError, "disagree"):
            self.host.plan()


class BoundaryTests(unittest.TestCase):
    def test_readiness_probe_rejects_redirects_instead_of_checking_another_target(self):
        followed = []
        class RedirectingBackend(BaseHTTPRequestHandler):
            def do_GET(self):
                if self.path == "/redirect":
                    self.send_response(302)
                    self.send_header("Location", "/target")
                    self.end_headers()
                else:
                    followed.append(self.path)
                    self.send_response(200)
                    self.end_headers()
                    self.wfile.write(b"Healthy")

            def log_message(self, *_):
                pass
        server = ThreadingHTTPServer(("127.0.0.1", 0), RedirectingBackend)
        worker = threading.Thread(target=server.serve_forever, daemon=True)
        worker.start()
        try:
            self.assertEqual((302, ""), deployment.Deployment("unused").request(f"http://127.0.0.1:{server.server_port}/redirect"))
            self.assertEqual([], followed)
        finally:
            server.shutdown()
            server.server_close()
            worker.join(timeout=2)

    def test_drain_budget_exceeds_nginx_and_long_request_deadlines(self):
        import re
        timeout = int(re.search(r"worker_shutdown_timeout (\d+)s;", (ROOT / "nginx.conf").read_text())[1])
        self.assertGreaterEqual(timeout, 190)
        self.assertGreater(deployment.DRAIN_SECONDS, timeout)

    def test_target_and_live_history_decide_mode_and_reject_unknown_or_changed_history(self):
        self.assertEqual("overlap", deployment.migration_mode("applied " + BASELINE, [BASELINE]))
        self.assertEqual("maintenance", deployment.migration_mode("pending " + BASELINE, []))
        for output, history in (("", []), ("applied " + BASELINE, [NEXT]), ("pending " + BASELINE, [BASELINE]),
                                ("applied " + BASELINE, {}), ("applied " + BASELINE, ["invalid"]),
                                ("pending " + BASELINE + "\napplied " + NEXT, [NEXT])):
            with self.subTest(output=output, history=history), self.assertRaises(deployment.DeploymentError):
                deployment.migration_mode(output, history)

    def test_loopback_unique_ports_and_matching_ssr_origin_are_required(self):
        self.assertEqual({"blue": [5000, 4000], "green": [5001, 4001]}, deployment.Deployment.ports(configuration()))
        for name in ("public", "duplicate", "wrong-api", "api-drift", "ssr-drift"):
            config = configuration()
            if name == "public":
                config["services"]["api-green"]["ports"][0]["host_ip"] = "0.0.0.0"
            elif name == "duplicate":
                config["services"]["api-green"]["ports"][0]["published"] = "5000"
            elif name == "wrong-api":
                config["services"]["client-ssr-green"]["environment"]["SSR_API_ORIGIN"] = "http://api:5000"
            elif name == "api-drift":
                config["services"]["api-green"]["read_only"] = False
            else:
                config["services"]["client-ssr-green"]["image"] = "unmatched-client"
            with self.subTest(name=name), self.assertRaises(deployment.DeploymentError):
                deployment.Deployment.ports(config)

    def test_memory_guard_accounts_for_swap_and_minimum_available_ram(self):
        host = deployment.Deployment("unused")
        for available, swap, accepted in ((512000, 512000, True), (100000, 2000000, False), (300000, 100000, False)):
            with self.subTest(available=available, swap=swap), patch.object(Path, "read_text", return_value=f"MemAvailable: {available} kB\nSwapFree: {swap} kB\n"):
                if accepted:
                    host.memory_headroom()
                else:
                    with self.assertRaises(deployment.DeploymentError):
                        host.memory_headroom()

    def test_checked_in_default_routes_match_blue_bootstrap_and_keep_metadata_loopback(self):
        self.assertEqual(deployment.routes("blue"), (ROOT / "nginx/sites-enabled/00-fooddiary-upstreams.conf").read_text())
        self.assertIn("listen 127.0.0.1:5090;", deployment.routes("green"))
        self.assertNotIn("listen 5090", deployment.routes("green"))
        grace = deployment.routes("green", affinity=True)
        retired = deployment.routes("green")
        self.assertIn("fooddiary_api_blue fooddiary_api_blue;", grace)
        self.assertNotIn("fooddiary_api_blue fooddiary_api_blue;", retired)
        self.assertIn("default fooddiary_api_green;", retired)


if __name__ == "__main__":
    unittest.main()
