"""Protect the production admin HTTPS origin across the host-to-Docker hop."""

from pathlib import Path
import json
import os
import re
import shutil
import subprocess
import tempfile
import unittest
from unittest.mock import patch

import configure_forwarded_proxy as proxy


ROOT = Path(__file__).resolve().parents[2]


class ForwardedProxyDeploymentTests(unittest.TestCase):
    @unittest.skipIf(os.name == "nt", "The production probe runs in Linux Bash")
    def test_admin_probe_recovers_from_http2_transport_failure(self):
        result, calls = self.run_admin_probe([("000", 16), ("401", 0)])
        self.assertEqual(0, result.returncode, "A transient HTTP2 framing error must not abort a healthy handover: " + result.stdout)
        self.assertEqual(2, calls)

    @unittest.skipIf(os.name == "nt", "The production probe runs in Linux Bash")
    def test_admin_probe_waits_for_upstream_startup(self):
        result, calls = self.run_admin_probe([("503", 0), ("401", 0)])
        self.assertEqual(0, result.returncode, result.stdout)
        self.assertEqual(2, calls)

    @unittest.skipIf(os.name == "nt", "The production probe runs in Linux Bash")
    def test_admin_probe_rejects_wrong_origin_without_retry(self):
        result, calls = self.run_admin_probe([("403", 0), ("401", 0)])
        self.assertNotEqual(0, result.returncode)
        self.assertEqual(1, calls, "A definitive origin rejection must remain a deployment failure")

    @unittest.skipIf(os.name == "nt", "The production probe runs in Linux Bash")
    def test_admin_probe_bounds_persistent_transport_errors(self):
        result, calls = self.run_admin_probe([("000", 16)] * 10)
        self.assertNotEqual(0, result.returncode)
        self.assertEqual(6, calls, "Transport failure must exhaust exactly the bounded six attempts")

    def run_admin_probe(self, responses):
        workflow = (ROOT / ".github/workflows/deploy.yml").read_text(encoding="utf-8")
        probe = workflow[workflow.index("# An invalid code must reach SSO validation"):workflow.index("# Retire the old slots")]
        with tempfile.TemporaryDirectory() as directory:
            count = Path(directory) / "calls"
            count.write_text("0")
            cases = "\n".join(f"{i}) printf '%s' '{status}'; return {code};;" for i, (status, code) in enumerate(responses, 1))
            fixture = '''set -euo pipefail
curl() {
  local attempt
  attempt=$(cat "$PROBE_CALLS")
  attempt=$((attempt + 1))
  printf '%s' "$attempt" > "$PROBE_CALLS"
  case "$attempt" in
''' + cases + '''
    *) return 99;;
  esac
}
sleep() { :; }
''' + probe
            result = subprocess.run(["bash", "-c", fixture], env={**os.environ, "PROBE_CALLS": str(count)}, capture_output=True, text=True, timeout=5)
            return result, int(count.read_text())

    def test_api_loads_discovered_proxy_before_recreation(self):
        compose = (ROOT / "docker-compose.yml").read_text(encoding="utf-8")
        # Inspect the complete service, including its indented children.
        api = compose[compose.index("\n  api:"):compose.index("\n  api-green:")]
        self.assertIn("path: .forwarded-proxy.env", api,
                      "API must load the exact Docker bridge proxy trust before admin HTTPS SSO")
        workflow = (ROOT / ".github/workflows/deploy.yml").read_text(encoding="utf-8")
        configure = workflow.index("python3 scripts/deploy/configure_forwarded_proxy.py")
        recreate = workflow.index("blue_green.py start-api")
        self.assertLess(configure, recreate)

    def test_deployment_checks_admin_origin_after_nginx_is_ready(self):
        workflow = (ROOT / ".github/workflows/deploy.yml").read_text(encoding="utf-8")
        probe = workflow[workflow.index("admin_sso_status=$(curl"):workflow.index("blue_green.py finish")]
        self.assertIn("--resolve admin.fooddiary.club:443:127.0.0.1", probe)
        self.assertIn("Origin: https://admin.fooddiary.club", probe)
        self.assertIn("X-FoodDiary-Client: admin", probe)
        self.assertIn("X-Requested-With: XMLHttpRequest", probe)
        self.assertIn('"$admin_sso_status" != "401"', probe)
        self.assertLess(workflow.index('if [ "$nginx_ready" != "true" ]'), workflow.index("admin_sso_status=$(curl"))

    def test_discovery_trusts_only_default_bridge_gateway(self):
        with patch.object(proxy.subprocess, "run", return_value=subprocess.CompletedProcess([], 0, "postgres-id\n")), patch.object(
            proxy, "docker_json", side_effect=[
                {"project_default": {"NetworkID": "default-id"}, "unrelated": {"NetworkID": "other-id"}},
                [self.network("172.29.0.1")],
                [self.network("172.30.0.1", name="other")],
            ],
        ) as inspect:
            self.assertEqual(["172.29.0.1"], proxy.discover_gateways())
            self.assertIn(unittest.mock.call("network", "inspect", "default-id"), inspect.call_args_list)

    def test_discovery_rejects_unsafe_or_missing_gateways(self):
        for gateway in [None, "0.0.0.0", "127.0.0.1", "8.8.8.8", "172.29.0.0/16", "not-an-ip"]:
            with self.subTest(gateway=gateway), patch.object(
                proxy.subprocess, "run", return_value=subprocess.CompletedProcess([], 0, "postgres-id\n"),
            ), patch.object(proxy, "docker_json", side_effect=[
                {"default": {"NetworkID": "default-id"}}, [self.network(gateway)],
            ]):
                with self.assertRaises(ValueError):
                    proxy.discover_gateways()

    def test_discovery_requires_one_default_bridge(self):
        for networks in [[], [self.network("172.29.0.1", name="other")],
                         [self.network("172.29.0.1"), self.network("172.30.0.1")]]:
            with self.subTest(networks=networks), patch.object(
                proxy.subprocess, "run", return_value=subprocess.CompletedProcess([], 0, "postgres-id\n"),
            ), patch.object(proxy, "docker_json", side_effect=[
                {str(i): {"NetworkID": str(i)} for i in range(len(networks))},
                *[[network] for network in networks],
            ]):
                with self.assertRaises(ValueError):
                    proxy.discover_gateways()

    def test_replaces_old_proxy_list_without_touching_operator_environment(self):
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            operator = root / ".env"
            operator.write_text("EXAMPLE=preserved\n", encoding="utf-8")
            output = root / ".forwarded-proxy.env"
            proxy.write_environment(output, ["172.29.0.1", "fd00::1"])
            proxy.write_environment(output, ["172.30.0.1"])
            value = output.read_text(encoding="utf-8")
            self.assertIn("ForwardedHeaders__KnownProxies__0=172.30.0.1\n", value)
            self.assertIn("ForwardedHeaders__ForwardLimit=1\n", value)
            self.assertNotIn("KnownProxies__1", value)
            self.assertNotIn("KnownNetworks", value)
            self.assertEqual("EXAMPLE=preserved\n", operator.read_text(encoding="utf-8"))

    @unittest.skipUnless(shutil.which("docker"), "Docker Compose CLI is required")
    def test_compose_passes_generated_trust_only_to_api(self):
        # Resolve the real Compose service with entirely synthetic configuration.
        compose = ROOT / "docker-compose.yml"
        required = re.findall(r"\$\{([A-Za-z0-9_]+):\?", compose.read_text(encoding="utf-8"))
        environment = {**os.environ, **dict.fromkeys(required, "synthetic-proxy-test")}
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            (root / ".env").write_text("", encoding="utf-8")
            proxy.write_environment(root / ".forwarded-proxy.env", ["172.29.0.1"])
            result = subprocess.run(
                ["docker", "compose", "--project-directory", directory, "-f", str(compose),
                 "--profile", "full", "--profile", "backend", "config", "--format", "json"],
                env=environment, check=True, capture_output=True, text=True, timeout=30,
            )
            services = json.loads(result.stdout)["services"]
            self.assertEqual("172.29.0.1", services["api"]["environment"]["ForwardedHeaders__KnownProxies__0"])
            self.assertEqual("1", services["api"]["environment"]["ForwardedHeaders__ForwardLimit"])
            for name, service in services.items():
                if name != "api":
                    self.assertNotIn("ForwardedHeaders__KnownProxies__0", service.get("environment", {}))

    @staticmethod
    def network(gateway, name="default"):
        return {"Driver": "bridge", "Labels": {"com.docker.compose.network": name},
                "IPAM": {"Config": [{"Gateway": gateway}]}}


if __name__ == "__main__":
    unittest.main()
