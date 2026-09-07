#!/usr/bin/env python3
"""Idempotent server rollout. Run as root; never prints credentials or mail."""
import json
import os
from pathlib import Path
import re
import secrets
import subprocess
import sys
import time
import urllib.request


def run(args, **kwargs):
    return subprocess.run(args, check=True, **kwargs)


def deploy(image):
    if not re.fullmatch(r"ghcr\.io/alexindima/food-diary/bugtriage@sha256:[a-f0-9]{64}", image):
        raise ValueError("A BugTriage image digest is required")
    os.umask(0o077)
    root = Path("/opt/fooddiary/bugtriage")
    root.mkdir(mode=0o700, parents=True, exist_ok=True)
    settings_path = root / ".env"
    values = {}
    if settings_path.exists():
        values = dict(line.split("=", 1) for line in settings_path.read_text().splitlines() if "=" in line)
    for key in ("BUGTRIAGE_OWNER_PASSWORD", "BUGTRIAGE_RUNTIME_PASSWORD", "BUGTRIAGE_API_KEY"):
        values.setdefault(key, secrets.token_hex(32))
    inbox = json.loads(subprocess.check_output(["docker", "inspect", "fooddiary-mail-inbox-1"]))[0]
    inbox_env = dict(item.split("=", 1) for item in inbox["Config"]["Env"])
    for name in ("Metadata", "Content"):
        value = inbox_env[f"MailInboxHttp__{name}ApiKey"]
        if not re.fullmatch(r"[A-Za-z0-9_+=/.-]{32,256}", value):
            raise ValueError("Unsupported MailInbox key encoding")
        values[f"MAIL_INBOX_{name.upper()}_API_KEY"] = value
    values["BUGTRIAGE_IMAGE_REF"] = image
    temporary = root / ".env.tmp"
    temporary.write_text("".join(f"{key}={value}\n" for key, value in values.items()))
    temporary.chmod(0o600)
    temporary.replace(settings_path)
    compose = ["docker", "compose", "--project-directory", str(root), "-f", str(root / "compose.yml")]
    run(compose + ["pull", "api", "bugtriage-postgres", "initialize"])
    run(compose + ["up", "-d", "--wait", "bugtriage-postgres"])
    run(compose + ["run", "--rm", "-T", "initialize"])
    # Passwords are generated hex and sent over stdin, never command arguments/logs.
    password = values["BUGTRIAGE_RUNTIME_PASSWORD"]
    if not re.fullmatch(r"[a-f0-9]{64}", password):
        raise ValueError("Unexpected runtime password format")
    sql = f"""
DO $$ BEGIN
 IF NOT EXISTS (SELECT 1 FROM pg_roles WHERE rolname='bugtriage_runtime') THEN
  CREATE ROLE bugtriage_runtime LOGIN;
 END IF;
END $$;
ALTER ROLE bugtriage_runtime PASSWORD '{password}';
REVOKE ALL ON DATABASE fooddiary_bugtriage FROM PUBLIC;
GRANT CONNECT ON DATABASE fooddiary_bugtriage TO bugtriage_runtime;
REVOKE CREATE ON SCHEMA public FROM PUBLIC;
GRANT USAGE ON SCHEMA public TO bugtriage_runtime;
GRANT SELECT, INSERT, UPDATE, DELETE ON bugtriage_reports TO bugtriage_runtime;
"""
    run(compose + ["exec", "-T", "bugtriage-postgres", "psql", "-U", "bugtriage_owner", "-d", "fooddiary_bugtriage", "-v", "ON_ERROR_STOP=1"],
        input=sql, text=True, stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL)
    run(compose + ["up", "-d", "--no-deps", "api"])
    request = urllib.request.Request("http://127.0.0.1:5099/api/bug-reports", headers={"X-BugTriage-Key": values["BUGTRIAGE_API_KEY"]})
    for attempt in range(30):
        try:
            with urllib.request.urlopen(request, timeout=5) as response:
                if response.status == 200:
                    print("BugTriage API and separate runtime database are ready.")
                    return
        except (OSError, ValueError):
            time.sleep(2)
    raise RuntimeError("BugTriage readiness check failed")


if __name__ == "__main__":
    try:
        deploy(sys.argv[1])
    except Exception:
        print("BugTriage deployment failed; inspect service state without printing secrets.", file=sys.stderr)
        sys.exit(1)
