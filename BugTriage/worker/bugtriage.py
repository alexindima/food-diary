"""Local bridge for a Codex task. Python standard library only; never runs mail attachments."""

import argparse
import email.policy
import json
import os
import sys
import urllib.error
import urllib.parse
import urllib.request
import uuid
from email.parser import BytesParser
from pathlib import Path

MAX_MIME = 10 * 1024 * 1024


class NoRedirect(urllib.request.HTTPRedirectHandler):
    def redirect_request(self, req, fp, code, msg, headers, newurl):
        raise ValueError("API redirects are not allowed")


def load_config(path):
    config = json.loads(Path(path).read_text(encoding="utf-8-sig"))
    parsed = urllib.parse.urlsplit(config["baseUrl"])
    if (parsed.scheme != "https" and not (
        parsed.scheme == "http" and parsed.hostname in ("localhost", "127.0.0.1", "::1")
    )) or parsed.username or parsed.password or parsed.query or parsed.fragment:
        raise ValueError("Use HTTPS or an SSH tunnel bound to loopback, without URL credentials")
    if not parsed.hostname or not 32 <= len(config["apiKey"]) <= 256:
        raise ValueError("Invalid BugTriage configuration")
    return config


def call_api(config, method, path, data=None, lease=None, binary=False):
    headers = {"X-BugTriage-Key": config["apiKey"]}
    if lease:
        headers["X-BugTriage-Lease"] = str(uuid.UUID(lease))
    payload = None
    if data is not None:
        headers["Content-Type"] = "application/json"
        payload = json.dumps(data).encode("utf-8")
    request = urllib.request.Request(config["baseUrl"].rstrip("/") + path, payload, headers, method=method)
    opener = urllib.request.build_opener(NoRedirect())
    with opener.open(request, timeout=30) as response:
        content = response.read(MAX_MIME + 1)
        if len(content) > MAX_MIME:
            raise ValueError("Response exceeds size limit")
        return content if binary else json.loads(content) if content else None


def save_private(path, content):
    path.parent.mkdir(mode=0o700, parents=True, exist_ok=True)
    # Keep files under the user's profile; on Windows inherit the private profile ACL.
    fd = os.open(path, os.O_WRONLY | os.O_CREAT | os.O_TRUNC, 0o600)
    with os.fdopen(fd, "wb") as target:
        target.write(content)


def extract_images(mime, directory):
    message = BytesParser(policy=email.policy.default).parsebytes(mime)
    images = []
    suffixes = {"image/png": ".png", "image/jpeg": ".jpg", "image/webp": ".webp", "image/gif": ".gif"}
    # Never use sender-supplied filenames, unpack archives, or render HTML/SVG.
    for index, part in enumerate(message.walk()):
        if index >= 100:
            break
        suffix = suffixes.get(part.get_content_type())
        if suffix:
            content = part.get_payload(decode=True)
            if content:
                path = directory / f"image-{index}{suffix}"
                save_private(path, content)
                images.append(str(path))
    return images


def claim(config, output):
    report = call_api(config, "POST", "/api/bug-reports/claim")
    if report is None:
        print("No pending bug reports.")
        return
    report_id = str(uuid.UUID(report["id"]))
    directory = output / report_id
    # Persist the lease before downloading MIME, so download failures are recoverable.
    lease_file = directory / "lease.json"
    save_private(lease_file, json.dumps(report, ensure_ascii=False, indent=2).encode("utf-8"))
    mime = call_api(config, "GET", f"/api/bug-reports/{report_id}/mime", lease=report["leaseToken"], binary=True)
    save_private(directory / "message.eml", mime)
    images = extract_images(mime, directory)
    context = {"reportId": report_id, "subject": report["subject"], "body": report["textBody"], "images": images}
    save_private(directory / "report.json", json.dumps(context, ensure_ascii=False, indent=2).encode("utf-8"))
    print(f"Claimed report: {directory / 'report.json'}")
    print(f"Lease file (private): {lease_file}")


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("action", choices=["list", "claim", "renew", "complete"])
    parser.add_argument("--config", type=Path, default=Path.home() / ".codex/secrets/food-diary.bugtriage.json")
    parser.add_argument("--output", type=Path, default=Path.home() / ".codex/bugtriage")
    parser.add_argument("--lease", type=Path)
    parser.add_argument("--result", type=Path)
    args = parser.parse_args()
    config = load_config(args.config)
    if args.action == "claim":
        claim(config, args.output.resolve())
    elif args.action == "list":
        print(json.dumps(call_api(config, "GET", "/api/bug-reports"), ensure_ascii=False, indent=2))
    else:
        if args.lease is None:
            parser.error("--lease is required")
        report = json.loads(args.lease.read_text(encoding="utf-8"))
        report_id = str(uuid.UUID(report["id"]))
        data = {"leaseToken": str(uuid.UUID(report["leaseToken"]))}
        if args.action == "complete":
            if args.result is None:
                parser.error("--result is required")
            result = json.loads(args.result.read_text(encoding="utf-8-sig"))
            data.update({key: result.get(key) for key in ("outcome", "summary", "mergeRequestUrl")})
        call_api(config, "POST", f"/api/bug-reports/{report_id}/{args.action}", data)
        print("Result saved." if args.action == "complete" else "Lease renewed.")


if __name__ == "__main__":
    try:
        main()
    except urllib.error.HTTPError as exc:
        print(f"BugTriage returned HTTP {exc.code}. For 409, stop: the lease is no longer owned.", file=sys.stderr)
        sys.exit(1)
    except (OSError, ValueError, KeyError, urllib.error.URLError):
        print("BugTriage request failed. Check the private configuration, lease, and server availability.", file=sys.stderr)
        sys.exit(1)
