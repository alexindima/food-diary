#!/usr/bin/env python3
"""Independent public HTTP/TLS probes; persist only fixed-name numeric results."""
from __future__ import annotations

import argparse
import concurrent.futures
import json
import os
import pathlib
import socket
import ssl
import time
import urllib.error
import urllib.parse
import urllib.request

TARGETS = {
    "site": "https://fooddiary.club/",
    "admin": "https://admin.fooddiary.club/",
    # Public readiness is intentionally private at ingress. A protected route's
    # Bearer challenge verifies the public API transport without test accounts.
    "api": "https://fooddiary.club/api/v1/products",
    "grafana": "https://grafana.fooddiary.club/api/health",
}


class RejectRedirect(urllib.request.HTTPRedirectHandler):
    def redirect_request(self, request, response, code, message, headers, new_url):
        raise urllib.error.HTTPError(request.full_url, code, "Unexpected redirect", headers, response)


def probe(item):
    name, url = item
    started = time.monotonic()
    result = {"target": name, "success": False, "http_status": 0, "duration_seconds": 0, "tls_days_remaining": 0}
    try:
        host = urllib.parse.urlsplit(url).hostname
        context = ssl.create_default_context()
        with socket.create_connection((host, 443), timeout=6) as connection:
            with context.wrap_socket(connection, server_hostname=host) as tls:
                expiry = ssl.cert_time_to_seconds(tls.getpeercert()["notAfter"])
                result["tls_days_remaining"] = max(0, (expiry - time.time()) / 86400)
        opener = urllib.request.build_opener(RejectRedirect())
        request = urllib.request.Request(url, headers={"User-Agent": "FoodDiary-Availability/1.0", "Cache-Control": "no-cache"})
        with opener.open(request, timeout=8) as response:
            result["http_status"] = response.status
            body = response.read(65537)
            expected = b"<html" in body.lower() if name in {"site", "admin"} else json.loads(body).get("database") == "ok"
            result["success"] = response.status == 200 and expected
    except urllib.error.HTTPError as error:
        result["http_status"] = error.code
        if name == "api" and error.code == 401 and error.headers.get("Content-Type", "").startswith("application/json"):
            try:
                envelope = json.loads(error.read(65536))
                result["success"] = isinstance(envelope, dict) and {"error", "message", "traceId"} <= envelope.keys()
            except (OSError, ValueError):
                pass
    except (OSError, ValueError, urllib.error.URLError):
        pass
    result["duration_seconds"] = round(time.monotonic() - started, 6)
    return result


def probes():
    with concurrent.futures.ThreadPoolExecutor(max_workers=4) as executor:
        return list(executor.map(probe, TARGETS.items()))


def transition(previous, observations, now):
    """Two failed observations page; successful observation confirms recovery.

    Cache loss restarts the failure grace period. A missing observation is not
    fabricated as success; runs that never execute remain a scheduler limitation.
    """
    states, changes = {}, []
    for observation in observations:
        name = observation["target"]
        old = previous.get("targets", {}).get(name, {})
        failures = 0 if observation["success"] else min(100, int(old.get("failures", 0))) + 1
        unhealthy = failures >= 2 or observation["tls_days_remaining"] < 14
        was_unhealthy = bool(old.get("alerting", False))
        # TLS is only an actionable expiry observation when HTTPS was reachable.
        if not observation["success"] and failures < 2:
            unhealthy = was_unhealthy
        if unhealthy != was_unhealthy:
            changes.append((name, unhealthy))
        states[name] = {"failures": failures, "alerting": unhealthy}
    return {"version": 1, "observed_at": now, "targets": states}, changes


def notify(changes):
    if not changes:
        return
    token, chat = os.environ["TELEGRAM_BOT_TOKEN"], os.environ["TELEGRAM_CHAT_ID"]
    lines = ["FoodDiary: внешняя проверка"]
    for name, unhealthy in changes:
        lines.append(("🔴 Недоступен или TLS истекает: " if unhealthy else "✅ Проверка снова проходит: ") + TARGETS[name])
    body = urllib.parse.urlencode({"chat_id": chat, "text": "\n".join(lines), "disable_web_page_preview": "true"}).encode()
    # Errors are deliberately not rendered: the endpoint contains a credential.
    request = urllib.request.Request("https://api.telegram.org/bot" + token + "/sendMessage", data=body)
    try:
        with urllib.request.build_opener(RejectRedirect()).open(request, timeout=15) as response:
            if not json.load(response).get("ok"):
                raise RuntimeError("Notification rejected")
    except (OSError, ValueError, urllib.error.URLError):
        raise RuntimeError("External-monitor notification failed") from None


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--state", type=pathlib.Path, default=pathlib.Path(".state/external-monitoring/state.json"))
    parser.add_argument("--result", type=pathlib.Path, default=pathlib.Path(".artifacts/external-monitoring/observation.json"))
    parser.add_argument("--notify", action="store_true")
    args = parser.parse_args()
    previous = {}
    if args.state.exists():
        try:
            previous = json.loads(args.state.read_text(encoding="utf-8"))
        except (OSError, ValueError):
            previous = {}
    observations = probes()
    state, changes = transition(previous, observations, time.time())
    if args.notify:
        notify(changes)
    args.state.parent.mkdir(parents=True, exist_ok=True)
    args.result.parent.mkdir(parents=True, exist_ok=True)
    args.state.write_text(json.dumps(state), encoding="utf-8")
    args.result.write_text(json.dumps({"observed_at": state["observed_at"], "observations": observations}, indent=2), encoding="utf-8")
    for observation in observations:
        print(json.dumps(observation))
    return int(any(not observation["success"] for observation in observations))


if __name__ == "__main__":
    raise SystemExit(main())
