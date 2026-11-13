"""Real Nginx handover, hub affinity and in-flight HTTP/WebSocket regression."""

import base64
import hashlib
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
from unittest.mock import patch

import blue_green
from test_api_disclosure_nginx import available_port, server_blocks


ROOT = Path(__file__).resolve().parents[2]


class Backend(BaseHTTPRequestHandler):
    protocol_version = "HTTP/1.1"

    def do_GET(self):
        host = self.headers.get("Host", "")
        self.server.hosts.append(host)
        if "_" in host:
            body = b"Invalid hostname"
            self.send_response(400)
            self.send_header("Content-Length", str(len(body)))
            self.end_headers()
            self.wfile.write(body)
            return
        if self.headers.get("Upgrade", "").lower() == "websocket":
            key = self.headers["Sec-WebSocket-Key"] + "258EAFA5-E914-47DA-95CA-C5AB0DC85B11"
            self.send_response(101)
            self.send_header("Upgrade", "websocket")
            self.send_header("Connection", "Upgrade")
            self.send_header("Sec-WebSocket-Accept", base64.b64encode(hashlib.sha1(key.encode()).digest()).decode())
            self.end_headers()
            self.server.release.wait(8)
            payload = self.server.color.encode()
            try:
                self.connection.sendall(bytes((0x81, len(payload))) + payload)
            except OSError:
                pass
            self.close_connection = True
            return
        if self.path == "/api/slow":
            self.server.entered.set()
            self.server.release.wait(8)
        body = self.server.color.encode()
        self.send_response(200)
        self.send_header("Content-Length", str(len(body)))
        self.end_headers()
        self.wfile.write(body)

    def do_POST(self):
        self.rfile.read(int(self.headers.get("Content-Length", "0")))
        self.do_GET()

    def log_message(self, *_):
        pass


@unittest.skipUnless(os.environ.get("FOODDIARY_NGINX_BINARY"), "Set FOODDIARY_NGINX_BINARY; Linux CI installs Nginx")
class NginxHandoverTests(unittest.TestCase):
    def setUp(self):
        temporary = tempfile.TemporaryDirectory(prefix="fd-handover-")
        self.addCleanup(temporary.cleanup)
        self.root = Path(temporary.name)
        (self.root / "logs").mkdir()
        (self.root / "temp").mkdir()
        (self.root / "html").mkdir()
        (self.root / "html/index.html").write_text("fixture")
        self.servers = []
        for color in ("blue", "blue-ssr", "green", "green-ssr"):
            backend = ThreadingHTTPServer(("127.0.0.1", 0), Backend)
            backend.color = color
            backend.hosts = []
            backend.entered = threading.Event()
            backend.release = threading.Event()
            threading.Thread(target=backend.serve_forever, daemon=True).start()
            self.servers.append(backend)
        self.addCleanup(self.close_backends)
        self.ports = {"blue": [self.servers[0].server_port, self.servers[1].server_port],
                      "green": [self.servers[2].server_port, self.servers[3].server_port]}
        self.port, self.probe = available_port(), available_port()
        source = (ROOT / "nginx/sites-enabled/fooddiary.club").read_text(encoding="utf-8")
        block = next(value for value in server_blocks(source) if "server_name fooddiary.club;" in value and "proxy_pass" in value)
        block = re.sub(r"^\s*(?:listen|http2|ssl_\w+|access_log|error_log)\s+[^;]+;", "", block, flags=re.MULTILINE)
        block = re.sub(r"root\s+[^;]+;", f'root "{(self.root / "html").as_posix()}";', block)
        block = block.replace("server {", f"server {{\nlisten 127.0.0.1:{self.port};", 1)
        self.routing = self.root / "routes.conf"
        self.configuration = self.root / "nginx.conf"
        self.configuration.write_text(f'''daemon off;
worker_processes 1;
worker_shutdown_timeout 5s;
pid "{(self.root / 'nginx.pid').as_posix()}";
error_log stderr;
events {{}}
http {{
map $http_upgrade $connection_upgrade {{ default upgrade; '' close; }}
include "{self.routing.as_posix()}";
{block}
}}
''', encoding="utf-8")
        self.write_routes("blue")
        self.command = [os.environ["FOODDIARY_NGINX_BINARY"], "-p", self.root.as_posix() + "/", "-c", self.configuration.as_posix()]
        self.flags = subprocess.CREATE_NO_WINDOW if os.name == "nt" else 0
        self.control("-t")
        self.log = (self.root / "process.log").open("wb")
        self.process = subprocess.Popen(self.command, cwd=self.root, stdout=self.log, stderr=self.log, creationflags=self.flags)
        self.addCleanup(self.close_nginx)
        self.until(lambda: self.request("/api/probe")[2] == b"blue")

    def control(self, *arguments):
        result = subprocess.run([*self.command, *arguments], cwd=self.root, capture_output=True, timeout=10, creationflags=self.flags)
        self.assertEqual(0, result.returncode, result.stderr.decode(errors="replace"))

    def close_backends(self):
        for backend in self.servers:
            backend.release.set()
            backend.shutdown()
            backend.server_close()

    def close_nginx(self):
        for backend in self.servers:
            backend.release.set()
        try:
            self.control("-s", "quit")
            self.process.wait(timeout=10)
        finally:
            if self.process.poll() is None:
                if os.name == "nt":
                    subprocess.run(["taskkill", "/PID", str(self.process.pid), "/T", "/F"], capture_output=True, timeout=10)
                else:
                    self.process.kill()
                self.process.wait(timeout=5)
            self.log.close()

    def write_routes(self, color, affinity=False):
        with patch.object(blue_green, "PROBE_PORT", self.probe):
            blue_green.write_atomic(self.routing, blue_green.routes(color, self.ports, affinity), 0o644)

    def request(self, path, cookie=None, method="GET"):
        connection = http.client.HTTPConnection("127.0.0.1", self.port, timeout=8)
        try:
            connection.request(method, path, headers={"Cookie": cookie} if cookie else {})
            response = connection.getresponse()
            return response.status, dict(response.getheaders()), response.read()
        finally:
            connection.close()

    def until(self, predicate):
        deadline = time.monotonic() + 8
        while True:
            try:
                if predicate():
                    return
            except OSError:
                pass
            if self.process.poll() is not None or time.monotonic() >= deadline:
                self.fail((self.root / "process.log").read_text(errors="replace"))
            time.sleep(0.025)

    def test_private_probes_send_valid_host_on_both_slots(self):
        for color, offset in (("blue", 0), ("green", 2)):
            with self.subTest(slot=color):
                self.write_routes(color)
                self.control("-t")
                self.control("-s", "reload")
                self.until(lambda: self.request("/api/probe")[2] == color.encode())
                for path, backend in (("/health/ready", offset), ("/internal/version", offset), ("/health/ssr", offset + 1)):
                    def probe_body():
                        connection = http.client.HTTPConnection("127.0.0.1", self.probe, timeout=8)
                        try:
                            connection.request("GET", path)
                            response = connection.getresponse()
                            self.assertEqual(200, response.status, "Private probe must use a valid upstream Host")
                            return response.read()
                        finally:
                            connection.close()
                    # Fresh connections can briefly reach retiring workers.
                    self.until(lambda: probe_body() == self.servers[backend].color.encode())
                    self.assertEqual("localhost", self.servers[backend].hosts[-1])
        self.assertEqual(404, self.request("/internal/version")[0])

    def test_routes_recover_when_nginx_switches_away_from_a_stopped_active_slot(self):
        send_response = Backend.send_response

        def close_upstream_connection(handler, code, message=None):
            send_response(handler, code, message)
            handler.send_header("Connection", "close")
            handler.close_connection = True

        paths = (
            ("/api/v1/products", "GET"),
            ("/api/v1/auth/telegram/bot/operations/ready", "GET"),
            ("/hubs/notifications/negotiate", "POST"),
        )

        def statuses():
            return [self.request(path, method=method)[0] for path, method in paths]

        with patch.object(Backend, "send_response", close_upstream_connection):
            self.assertEqual([200, 200, 200], statuses())
            active = self.servers[0]
            active.shutdown()
            active.server_close()
            self.assertEqual([502, 502, 502], statuses())

            self.write_routes("green")
            self.control("-s", "reload")
            # Fresh connections can still reach retiring workers during reload.
            def routes_serve_green():
                for path, method in paths:
                    status, _, body = self.request(path, method=method)
                    if (status, body) != (200, b"green"):
                        return False
                return True

            self.until(routes_serve_green)

    def test_reload_preserves_inflight_http_and_websocket_and_hub_affinity(self):
        _, headers, body = self.request("/hubs/notifications/negotiate", method="POST")
        self.assertEqual(b"blue", body)
        cookie = headers["Set-Cookie"].split(";")[0]
        self.assertEqual("fd_api_slot=fooddiary_api_blue", cookie)
        self.assertIn("HttpOnly", headers["Set-Cookie"])
        self.assertIn("Secure", headers["Set-Cookie"])
        self.assertIn("SameSite=None", headers["Set-Cookie"], "Routing affinity must support the embedded Telegram client")
        socket_client = socket.create_connection(("127.0.0.1", self.port), timeout=8)
        self.addCleanup(socket_client.close)
        socket_client.sendall(("GET /hubs/notifications?id=owned HTTP/1.1\r\nHost: fooddiary.club\r\n"
                               "Upgrade: websocket\r\nConnection: Upgrade\r\nSec-WebSocket-Version: 13\r\n"
                               "Sec-WebSocket-Key: dGhlIHNhbXBsZSBub25jZQ==\r\nCookie: " + cookie + "\r\n\r\n").encode())
        stream = socket_client.makefile("rb")
        self.addCleanup(stream.close)
        self.assertIn(b"101", stream.readline())
        while stream.readline().strip():
            pass
        result = []
        slow = threading.Thread(target=lambda: result.append(self.request("/api/slow")))
        slow.start()
        self.assertTrue(self.servers[0].entered.wait(3))
        self.write_routes("green", affinity=True)
        self.control("-t")
        self.control("-s", "reload")
        self.until(lambda: self.request("/api/probe")[2] == b"green")
        self.assertEqual(b"blue", self.request("/hubs/notifications?id=owned", cookie)[2])
        # HUP starts new workers before the old workers process QUIT. Distinct
        # fresh connections can briefly reach either generation during that hop.
        self.until(lambda: self.request("/hubs/notifications/negotiate", method="POST")[2] == b"green")
        self.until(lambda: self.request("/explore")[2] == b"green-ssr")
        self.servers[0].release.set()
        self.assertEqual(b"\x81\x04blue", stream.read(6), "Existing upgraded connection must survive reload")
        slow.join(timeout=3)
        self.assertFalse(slow.is_alive())
        self.assertEqual((200, b"blue"), (result[0][0], result[0][2]))
        self.write_routes("green")
        self.control("-s", "reload")
        # Check the payload and renewed affinity on the same connection; another
        # fresh connection can still see the retiring worker generation.
        def retired_cookie_routes_green():
            status, headers, body = self.request("/hubs/notifications?id=stale", cookie)
            return (status, body, headers.get("Set-Cookie", "").split(";")[0]) == (
                200, b"green", "fd_api_slot=fooddiary_api_green")
        self.until(retired_cookie_routes_green)
        for _ in range(5):
            self.assertEqual((200, b"green"), (lambda value: (value[0], value[2]))(self.request("/api/probe")))
        self.assertEqual(404, self.request("/internal/version")[0])


if __name__ == "__main__":
    unittest.main()
