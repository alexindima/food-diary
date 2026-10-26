"""Exercise checked-in proxy locations with a local nginx binary (no production I/O)."""

import argparse
import http.client
from http.server import BaseHTTPRequestHandler, ThreadingHTTPServer
import os
from pathlib import Path
import re
import socket
import subprocess
import tempfile
import threading
import time
import unittest


ROOT = Path(__file__).resolve().parents[2]
METADATA = b'{"commitSha":"private-deployment-marker"}'


class Backend(BaseHTTPRequestHandler):
    def do_GET(self):
        self.send_response(200)
        self.end_headers()
        self.wfile.write(METADATA if self.path == "/internal/version" else b"public-api")

    def log_message(self, *_):
        pass


def server_blocks(source):
    for match in re.finditer(r"^server\s*\{", source, re.MULTILINE):
        depth = 1
        end = match.end()
        while depth:
            depth += (source[end] == "{") - (source[end] == "}")
            end += 1
        yield source[match.start():end]


def available_port():
    with socket.socket() as probe:
        probe.bind(("127.0.0.1", 0))
        return probe.getsockname()[1]


class ApiDisclosureNginxTests(unittest.TestCase):
    def setUp(self):
        if not os.environ.get("FOODDIARY_NGINX_BINARY"):
            self.skipTest("Set FOODDIARY_NGINX_BINARY to a local nginx binary")

    def test_public_proxies_keep_metadata_internal(self):
        production = (ROOT / "nginx/sites-enabled/fooddiary.club").read_text(encoding="utf-8")
        standalone = (ROOT / "FoodDiary.Web.Client/nginx.conf").read_text(encoding="utf-8")
        telegram = (ROOT / "FoodDiary.Web.Client/nginx.telegram-test.conf").read_text(encoding="utf-8")
        cases = {
            "public": next(block for block in server_blocks(production) if "server_name fooddiary.club;" in block and "proxy_pass" in block),
            "admin": next(block for block in server_blocks(production) if "server_name admin.fooddiary.club;" in block and "proxy_pass" in block),
            "standalone": next(server_blocks(standalone)),
            "telegram": next(server_blocks(telegram)),
        }
        backend = ThreadingHTTPServer(("127.0.0.1", 0), Backend)
        worker = threading.Thread(target=backend.serve_forever, daemon=True)
        worker.start()
        self.addCleanup(backend.server_close)
        self.addCleanup(backend.shutdown)
        for label, block in cases.items():
            with self.subTest(proxy=label), tempfile.TemporaryDirectory(prefix="fd-nginx-disclosure-") as directory:
                self.verify_proxy(label, block, Path(directory), backend.server_port)

    def verify_proxy(self, label, block, directory, backend_port):
        port = available_port()
        (directory / "logs").mkdir()
        (directory / "temp").mkdir()
        (directory / "html").mkdir()
        for filename in ("index.html", "index.csr.html"):
            (directory / "html" / filename).write_text("frontend-shell", encoding="utf-8")
        # Keep real location matching/proxy directives; replace only platform,
        # TLS, filesystem and backend addresses with isolated fixture resources.
        block = re.sub(r"^\s*(?:listen|http2|ssl_\w+|access_log|error_log)\s+[^;]+;", "", block, flags=re.MULTILINE)
        block = re.sub(r"root\s+[^;]+;", f'root "{(directory / "html").as_posix()}";', block)
        block = re.sub(r"proxy_pass\s+[^;]+;", f"proxy_pass http://127.0.0.1:{backend_port};", block)
        block = block.replace("server {", f"server {{\nlisten 127.0.0.1:{port};", 1)
        tokens = (re.search(r"^\s*server_tokens\s+[^;]+;", (ROOT / "nginx.conf").read_text(), re.MULTILINE)
                  if label in ("public", "admin") else None)
        configuration = directory / "nginx.conf"
        configuration.write_text("daemon off;\nmaster_process off;\npid nginx.pid;\nerror_log stderr;\nevents {}\nhttp {\n"
                                 + (tokens.group(0) if tokens else "")
                                 + "\nmap $http_upgrade $connection_upgrade { default upgrade; '' close; }\n"
                                 + block + "\n}\n", encoding="utf-8")
        command = [os.environ["FOODDIARY_NGINX_BINARY"], "-p", directory.as_posix() + "/", "-c", configuration.as_posix()]
        checked = subprocess.run([*command, "-t"], capture_output=True, timeout=15)
        self.assertEqual(0, checked.returncode, checked.stderr.decode(errors="replace"))
        flags = subprocess.CREATE_NO_WINDOW if os.name == "nt" else 0
        with (directory / "process.log").open("wb") as log:
            process = subprocess.Popen(command, stdout=log, stderr=log, creationflags=flags)
            try:
                deadline = time.monotonic() + 15
                while True:
                    try:
                        self.request(port, "/")
                        break
                    except OSError:
                        if process.poll() is not None or time.monotonic() >= deadline:
                            self.fail((directory / "process.log").read_text(errors="replace"))
                        time.sleep(0.05)
                if label != "standalone":
                    status, _, body = self.request(port, "/api/probe")
                    self.assertEqual((200, b"public-api"), (status, body))
                for path in ("/internal/version", "/internal/version/", "/INTERNAL/VERSION", "/internal%2fversion",
                             "/api/../internal/version", "/api/%2e%2e/internal/version", "/api/%2e%2e%2finternal/version"):
                    status, headers, body = self.request(port, path)
                    self.assertEqual(404, status, (label, path, body))
                    self.assertNotIn(b"private-deployment-marker", body)
                    self.assertEqual("nginx", headers.get("server"), (label, path, headers))
            finally:
                process.terminate()
                try:
                    process.wait(timeout=5)
                except subprocess.TimeoutExpired:
                    process.kill()
                    process.wait(timeout=5)

    @staticmethod
    def request(port, path):
        connection = http.client.HTTPConnection("127.0.0.1", port, timeout=3)
        try:
            # http.client preserves raw dot segments/escapes for nginx to normalize.
            connection.request("GET", path)
            response = connection.getresponse()
            return response.status, {name.lower(): value for name, value in response.getheaders()}, response.read()
        finally:
            connection.close()


if __name__ == "__main__":
    parser = argparse.ArgumentParser()
    parser.add_argument("--nginx", required=True)
    arguments, remaining = parser.parse_known_args()
    os.environ["FOODDIARY_NGINX_BINARY"] = str(Path(arguments.nginx).resolve())
    unittest.main(argv=[__file__, *remaining])
