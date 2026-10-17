import base64
import json
import os
import ssl
import subprocess
import sys
import tempfile
import unittest
import urllib.error
import urllib.request
from pathlib import Path
from unittest.mock import patch

import bugtriage
import ssh_transport

KEY = "synthetic-key-" + "x" * 32
CONFIG = {"baseUrl": "http://127.0.0.1:15099", "apiKey": KEY}
LEASE = "06bc7f7e-8348-4938-a4bb-a7a53c059f20"


class TransportTests(unittest.TestCase):
    def pipe_request(self, response, *, method="GET", path="/api/bug-reports", data=None, binary=False, stall=False):
        # A real owned subprocess exercises pipe I/O and the standard HTTP parser.
        # It stands in for ssh AFTER authentication; dedicated SSH integration is separate.
        with tempfile.TemporaryDirectory() as directory:
            capture = Path(directory) / "request.bin"
            program = f"""import base64,sys,time
headers=b''
while not headers.endswith(b'\\r\\n\\r\\n'):
 part=sys.stdin.buffer.read(1)
 if not part: sys.exit(1)
 headers+=part
length=next((int(h.split(b':',1)[1]) for h in headers.split(b'\\r\\n') if h.lower().startswith(b'content-length:')),0)
body=sys.stdin.buffer.read(length)
open({str(capture)!r},'wb').write(headers+body)
{'time.sleep(60)' if stall else 'pass'}
sys.stdout.buffer.write(base64.b64decode({base64.b64encode(response)!r}));sys.stdout.buffer.flush()
"""
            with patch.object(ssh_transport, "ssh_command", return_value=[sys.executable, "-u", "-c", program]), \
                    patch("socket.create_connection", side_effect=AssertionError("No local TCP connection allowed")):
                result = bugtriage.call_api(CONFIG, method, path, data, lease=LEASE, binary=binary)
            return result, capture.read_bytes()

    def test_direct_call_rejects_plaintext_and_ambiguous_urls_before_transport(self):
        urls = ["http://example.com", "http://127.0.0.1:80", "http://127.0.0.1:12345",
                "http://127.0.0.1:15099/other", "http://127.0.0.1:15099?x=1", "http://127.0.0.1:15099#x",
                "http://127.1:15099", "http://2130706433:15099", "http://localhost.:15099",
                "https://user:pass@example.com", "https://example.com:99999", "\nhttps://example.com",
                "https://exa\tmple.com", "file:///tmp/key"]
        with patch.object(ssh_transport, "request") as ssh, patch.object(urllib.request, "build_opener") as https:
            for url in urls:
                with self.subTest(url=url), self.assertRaises(ValueError):
                    bugtriage.call_api({**CONFIG, "baseUrl": url}, "GET", "/api/bug-reports")
            ssh.assert_not_called()
            https.assert_not_called()

    def test_loopback_selectors_never_consult_a_listener_or_proxy(self):
        for url in ("http://127.0.0.1:15099", "http://localhost:5099/", "http://[::1]:15099"):
            with self.subTest(url=url), patch.object(ssh_transport, "request", return_value=b"[]") as ssh, \
                    patch.object(urllib.request, "build_opener", side_effect=AssertionError("No local HTTP")):
                self.assertEqual([], bugtriage.call_api({**CONFIG, "baseUrl": url}, "GET", "/api/bug-reports"))
                self.assertEqual(KEY, ssh.call_args.args[3]["X-BugTriage-Key"])

    def test_ssh_command_authenticates_host_without_shell_or_shared_control_socket(self):
        command = ssh_transport.ssh_command()
        self.assertEqual(["-W", "127.0.0.1:5099", "fooddiary-bugtriage"], command[-3:])
        for option in ("BatchMode=yes", "StrictHostKeyChecking=yes", "ControlPath=none", "PermitLocalCommand=no", "RemoteCommand=none"):
            self.assertIn(option, command)
        self.assertNotIn(KEY, command)
        self.assertNotIn(LEASE, command)

    def test_ssh_json_payload_and_private_headers(self):
        result, request = self.pipe_request(b"HTTP/1.1 200 OK\r\nContent-Length: 2\r\n\r\n{}", method="PUT", data={"summary": "audit"})
        self.assertEqual({}, result)
        self.assertIn(b"PUT /api/bug-reports HTTP/1.1\r\n", request)
        self.assertIn(KEY.encode(), request)
        self.assertIn(LEASE.encode(), request)
        self.assertEqual({"summary": "audit"}, json.loads(request.split(b"\r\n\r\n", 1)[1]))

    def test_binary_chunked_empty_and_error_status_semantics(self):
        for body, headers in ((b"\x00\xffmime\r\n", b"Content-Length: 8\r\n"), (b"3\r\nraw\r\n0\r\n\r\n", b"Transfer-Encoding: chunked\r\n")):
            with self.subTest(headers=headers):
                result, _ = self.pipe_request(b"HTTP/1.1 200 OK\r\n" + headers + b"\r\n" + body, binary=True)
                self.assertEqual(b"raw" if headers.startswith(b"Transfer") else body, result)
        self.assertIsNone(self.pipe_request(b"HTTP/1.1 204 No Content\r\n\r\n")[0])
        for status in (401, 404, 409, 503):
            with self.subTest(status=status), self.assertRaises(urllib.error.HTTPError) as raised:
                self.pipe_request(f"HTTP/1.1 {status} Error\r\nContent-Length: 0\r\n\r\n".encode())
            self.assertEqual(status, raised.exception.code)

    def test_redirect_malformed_truncated_and_oversized_responses_fail(self):
        for response, error in ((b"HTTP/1.1 302 Found\r\nLocation: http://localhost:15099\r\nContent-Length: 0\r\n\r\n", ValueError),
                                (b"not http\r\n\r\n", OSError),
                                (b"HTTP/1.1 200 OK\r\nContent-Length: 20\r\n\r\n{}", OSError)):
            with self.subTest(response=response[:30]), self.assertRaises(error):
                self.pipe_request(response)
        with patch.object(ssh_transport, "MAX_RESPONSE", 16), self.assertRaises(ValueError):
            self.pipe_request(b"HTTP/1.1 200 OK\r\nContent-Length: 17\r\n\r\n" + b"x" * 17)

    def test_deadline_kills_and_reaps_owned_process(self):
        real_popen = subprocess.Popen
        processes = []

        def create(*args, **kwargs):
            self.assertFalse(kwargs["shell"])
            self.assertEqual(subprocess.DEVNULL, kwargs["stderr"])
            process = real_popen(*args, **kwargs)
            processes.append(process)
            return process

        with patch.object(ssh_transport, "TIMEOUT", 0.2), patch.object(subprocess, "Popen", side_effect=create), self.assertRaises(OSError):
            self.pipe_request(b"", stall=True)
        self.assertIsNotNone(processes[0].poll())
        self.assertTrue(processes[0].stdin.closed)
        self.assertTrue(processes[0].stdout.closed)

    def test_https_disables_ambient_proxies_and_keeps_certificate_verification(self):
        with patch.dict(os.environ, {"HTTPS_PROXY": "http://127.0.0.1:15099", "HTTP_PROXY": "http://127.0.0.1:15099"}), \
                patch.object(urllib.request.OpenerDirector, "open") as open_request, \
                patch.object(urllib.request, "build_opener", wraps=urllib.request.build_opener) as build:
            open_request.return_value.__enter__.return_value.read.return_value = b"[]"
            self.assertEqual([], bugtriage.call_api({**CONFIG, "baseUrl": "https://example.com"}, "GET", "/api/bug-reports"))
            self.assertEqual({}, build.call_args.args[0].proxies)
            self.assertIsInstance(build.call_args.args[1], bugtriage.NoRedirect)
            opener = build(*build.call_args.args)
            self.assertFalse(any(isinstance(handler, urllib.request.ProxyHandler) for handler in opener.handlers))
            context = next(handler for handler in opener.handlers if isinstance(handler, urllib.request.HTTPSHandler))._context
            self.assertTrue(context.check_hostname)
            self.assertEqual(ssl.CERT_REQUIRED, context.verify_mode)
            self.assertEqual(30, open_request.call_args.kwargs["timeout"])
        with self.assertRaises(ValueError):
            bugtriage.NoRedirect().redirect_request(None, None, 302, "", {}, "https://other.example")


if __name__ == "__main__":
    unittest.main()
