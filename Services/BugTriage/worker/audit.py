"""Publish sanitized production audit results; keep failed deliveries for an identical retry."""

import argparse
import datetime
import json
import os
import sys
import tempfile
import urllib.error
import uuid
from pathlib import Path

from bugtriage import call_api, load_config, save_private


UTC = datetime.timezone.utc
OUTCOMES = {"clean", "findings", "incomplete", "failed"}
FINDING_OUTCOMES = {"needs_information", "not_confirmed", "duplicate", "draft_ready", "failed"}


def timestamp(value):
    parsed = datetime.datetime.fromisoformat(value.replace("Z", "+00:00"))
    if parsed.tzinfo is None:
        raise ValueError("An explicit UTC offset is required")
    return parsed.astimezone(UTC)


def save_json(path, value):
    save_private(path, json.dumps(value, ensure_ascii=False, indent=2).encode("utf-8"))


def save_completion(path, value):
    # Publish a fully written file without replacing another worker's prepared result.
    descriptor, temporary = tempfile.mkstemp(prefix=".completion-", dir=path.parent)
    try:
        with os.fdopen(descriptor, "wb") as target:
            target.write(json.dumps(value, ensure_ascii=False, indent=2).encode("utf-8"))
            target.flush()
            os.fsync(target.fileno())
        try:
            os.link(temporary, path)
        except FileExistsError:
            if json.loads(path.read_text(encoding="utf-8")) != value:
                raise ValueError("An existing completion cannot be overwritten") from None
    finally:
        Path(temporary).unlink(missing_ok=True)


def load_run(path, now=None):
    run = json.loads(path.read_text(encoding="utf-8-sig"))
    run["id"] = str(uuid.UUID(run["id"]))
    start, end = timestamp(run["fromUtc"]), timestamp(run["toUtc"])
    if not datetime.timedelta(0) < end - start <= datetime.timedelta(days=7):
        raise ValueError("Invalid audit interval")
    if timestamp(run["expiresAtUtc"]) <= (now or datetime.datetime.now(UTC)):
        raise ValueError("Local audit retention has expired")
    return run


def register(config, run):
    call_api(config, "PUT", f"/api/audit-runs/{run['id']}/start",
             {key: run[key] for key in ("fromUtc", "toUtc")})


def start(config, output, now=None):
    end = (now or datetime.datetime.now(UTC)).astimezone(UTC)
    retention_days = int(config.get("auditRetentionDays", 30))
    if not 1 <= retention_days <= 90:
        raise ValueError("Invalid audit retention")
    run = {"id": str(uuid.uuid4()), "fromUtc": (end - datetime.timedelta(days=1)).isoformat(),
           "toUtc": end.isoformat(), "expiresAtUtc": (end + datetime.timedelta(days=retention_days)).isoformat()}
    path = output / run["id"] / "run.json"
    save_json(path, run)
    # The fixed interval and ID survive unavailable endpoints or a lost response.
    print(f"Audit run: {path}")
    register(config, run)
    print("Audit start saved.")
    return path


def build_completion(run, result):
    from urllib.parse import urlsplit

    outcome = result["outcome"]
    summary = result["summary"]
    findings = result["findings"]
    if outcome not in OUTCOMES or not isinstance(summary, str) or not summary.strip() or len(summary) > 8000:
        raise ValueError("Invalid audit outcome")
    if not isinstance(findings, list) or len(findings) > 20:
        raise ValueError("Invalid findings")
    if (outcome == "clean" and findings) or (outcome == "findings" and not findings):
        raise ValueError("The outcome must match the findings")
    normalized = []
    for index, finding in enumerate(findings):
        title, detail, status = finding["title"], finding["summary"], finding["outcome"]
        url = finding.get("mergeRequestUrl")
        if (not isinstance(title, str) or not title.strip() or len(title) > 1000 or
                not isinstance(detail, str) or not detail.strip() or len(detail) > 8000 or
                status not in FINDING_OUTCOMES):
            raise ValueError("Invalid finding")
        if url is not None:
            parsed = urlsplit(url)
            if len(url) > 2048 or parsed.scheme != "https" or not parsed.hostname or parsed.username or parsed.password:
                raise ValueError("Invalid draft URL")
        if status == "draft_ready" and url is None:
            raise ValueError("A draft URL is required")
        normalized.append({"id": str(uuid.uuid5(uuid.UUID(run["id"]), str(index))),
                           "title": title, "outcome": status, "summary": detail, "mergeRequestUrl": url})
    completion = {"outcome": outcome, "summary": summary, "findings": normalized}
    if len(json.dumps(completion).encode("utf-8")) > 65536:
        raise ValueError("Audit result exceeds the request limit")
    return completion


def deliver(config, path):
    run = load_run(path)
    directory = path.parent
    completion = json.loads((directory / "completion.json").read_text(encoding="utf-8"))
    if (directory / "published.json").exists():
        print("Audit result was already saved.")
        return
    register(config, run)
    call_api(config, "PUT", f"/api/audit-runs/{run['id']}/complete", completion)
    save_json(directory / "published.json", {"publishedAtUtc": datetime.datetime.now(UTC).isoformat()})
    print("Audit result saved.")


def complete(config, path, result):
    run = load_run(path)
    completion = build_completion(run, result)
    pending = path.parent / "completion.json"
    save_completion(pending, completion)
    deliver(config, path)


def retry_pending(config, output):
    # Keep retries bounded. A failed publication must not rerun the audit or create another PR.
    pending = sorted(path for path in output.glob("*/run.json") if (path.parent / "completion.json").exists()
                     and not (path.parent / "published.json").exists())[:20]
    failures = 0
    for path in pending[:20]:
        try:
            deliver(config, path)
        except (OSError, ValueError, KeyError, TypeError, urllib.error.URLError):
            failures += 1
    if failures:
        raise ValueError("Some audit results remain pending")
    print(f"Pending audit results processed: {min(len(pending), 20)}")


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("action", choices=["start", "complete", "retry"])
    parser.add_argument("--config", type=Path, default=Path.home() / ".codex/secrets/food-diary.bugtriage.json")
    parser.add_argument("--output", type=Path, default=Path.home() / ".codex/production-audit")
    parser.add_argument("--run", type=Path)
    parser.add_argument("--result", type=Path)
    args = parser.parse_args()
    config = load_config(args.config)
    if args.action == "start":
        start(config, args.output.resolve())
    elif args.action == "retry":
        retry_pending(config, args.output.resolve())
    else:
        if args.run is None or args.result is None:
            parser.error("--run and --result are required")
        complete(config, args.run.resolve(), json.loads(args.result.read_text(encoding="utf-8-sig")))


if __name__ == "__main__":
    try:
        main()
    except urllib.error.HTTPError as error:
        print(f"Audit journal returned HTTP {error.code}; saved results remain local.", file=sys.stderr)
        sys.exit(1)
    except (OSError, ValueError, KeyError, TypeError, urllib.error.URLError):
        print("Audit publication failed. Check service availability and the private configuration; do not rerun a completed audit.",
              file=sys.stderr)
        sys.exit(1)
