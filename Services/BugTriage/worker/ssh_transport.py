"""HTTP over an owned, authenticated OpenSSH direct-tcpip channel; no local listener."""

import http.client
import io
import os
import subprocess
import sys
import threading
import urllib.error
from contextlib import suppress
from pathlib import Path

TIMEOUT = 30
MAX_RESPONSE = 10 * 1024 * 1024


def ssh_command():
    # Use the installed Windows client rather than searching the working directory.
    executable = str(Path(os.environ["SystemRoot"]) / "System32/OpenSSH/ssh.exe") if os.name == "nt" else "ssh"
    return [executable, "-T", "-a", "-o", "BatchMode=yes", "-o", "StrictHostKeyChecking=yes",
            "-o", "ConnectTimeout=10", "-o", "ConnectionAttempts=1",
            "-o", "ControlMaster=no", "-o", "ControlPath=none", "-o", "ControlPersist=no",
            "-o", "PermitLocalCommand=no", "-o", "RemoteCommand=none",
            "-W", "127.0.0.1:5099", "fooddiary-bugtriage"]


class PipeSocket:
    def __init__(self, process):
        self.process = process

    def sendall(self, data):
        self.process.stdin.write(data)
        self.process.stdin.flush()

    def makefile(self, mode):
        if mode != "rb":
            raise ValueError("Unsupported pipe mode")
        return self.process.stdout

    def close(self):
        self.process.stdin.close()


class PipeConnection(http.client.HTTPConnection):
    def connect(self):
        # HTTPConnection must never reconnect to an unauthenticated local socket.
        raise OSError("SSH channel is closed")


def request(method, path, payload, headers):
    process = subprocess.Popen(ssh_command(), stdin=subprocess.PIPE, stdout=subprocess.PIPE,
                               stderr=subprocess.DEVNULL, shell=False,
                               creationflags=subprocess.CREATE_NO_WINDOW if os.name == "nt" else 0)
    expired = threading.Event()

    def expire():
        expired.set()
        with suppress(OSError):
            process.kill()

    timer = threading.Timer(TIMEOUT, expire)
    timer.daemon = True
    connection = PipeConnection("127.0.0.1", 5099)
    connection.sock = PipeSocket(process)
    timer.start()
    try:
        connection.request(method, path, payload, {**headers, "Connection": "close"})
        with connection.getresponse() as response:
            content = response.read(MAX_RESPONSE + 1)
            if len(content) > MAX_RESPONSE:
                raise ValueError("Response exceeds size limit")
            if response.length not in (None, 0):
                raise OSError("Incomplete SSH HTTP response")
            if expired.is_set():
                raise OSError("SSH request timed out")
            if 300 <= response.status < 400:
                raise ValueError("API redirects are not allowed")
            if response.status >= 400:
                raise urllib.error.HTTPError("ssh://fooddiary-bugtriage" + path, response.status,
                                             "BugTriage API request failed", response.headers, io.BytesIO(content))
            return content
    except (http.client.HTTPException, BrokenPipeError) as from_error:
        raise OSError("SSH HTTP request failed") from from_error
    finally:
        timer.cancel()
        # Kill before closing buffered streams, including on a partial write/read.
        if process.poll() is None:
            with suppress(OSError):
                process.kill()
        process.wait(timeout=5)
        with suppress(OSError):
            connection.close()
        with suppress(OSError):
            process.stdin.close()
        process.stdout.close()


def main():
    try:
        request("GET", "/api/bug-reports", None, {})
    except urllib.error.HTTPError as error:
        if error.code == 401:
            print("BugTriage authenticated SSH transport is available.")
            return
        raise OSError("Unexpected BugTriage readiness response") from None
    raise OSError("BugTriage must require an API key")


if __name__ == "__main__":
    try:
        main()
    except (OSError, ValueError):
        print("BugTriage SSH transport unavailable; check the restricted alias and pinned host key.", file=sys.stderr)
        sys.exit(1)
