"""Bounded read-only collection. Inject the existing authenticated GET-JSON transport.

No credentials, arbitrary response bodies, log lines or query text are persisted.
The caller still owns host correlation, interval metrics and incident conclusions.
"""

from collections import Counter
from datetime import datetime, timezone
import json
import math
import re


def utc_nanoseconds(value):
    instant = datetime.fromisoformat(value.replace("Z", "+00:00"))
    if instant.tzinfo is None:
        raise ValueError("Audit timestamps must have an explicit timezone")
    delta = instant.astimezone(timezone.utc) - datetime(1970, 1, 1, tzinfo=timezone.utc)
    return ((delta.days * 86400 + delta.seconds) * 1_000_000 + delta.microseconds) * 1000


def collect_audit(get_json, from_utc, to_utc, *, selector, service_names=(),
                  metrics=None, limit=1000, slice_seconds=3600, max_requests=256,
                  deployed_revision=None):
    """get_json(path, params) must use trusted-origin GET, timeouts and no redirects.

    selector and metrics come from current baselines, not previous incident text.
    A complete configured collection is not proof of complete audit coverage.
    """
    start, end = utc_nanoseconds(from_utc), utc_nanoseconds(to_utc)
    if (start >= end or type(limit) is not int or not 1 <= limit <= 5000
            or type(max_requests) is not int or max_requests < 1
            or not isinstance(slice_seconds, (int, float)) or not math.isfinite(slice_seconds)
            or slice_seconds < 1e-9):
        raise ValueError("Invalid audit window or collection bounds")
    if deployed_revision is not None and not re.fullmatch(r"[a-fA-F0-9]{40}", deployed_revision):
        raise ValueError("Deployed revision must be an observed full commit SHA")
    services = set(service_names)
    result = {"schemaVersion": 1, "fromUtc": from_utc, "toUtc": to_utc,
              "deployedRevision": deployed_revision, "revisionStatus": "observed" if deployed_revision else "unknown",
              "collectionComplete": True, "requests": 0, "sources": [], "gaps": [],
              "uncoveredEvidence": ["host correlation", "unrequested interval metrics", "incident validation"]}

    def request(path, parameters):
        if result["requests"] >= max_requests:
            raise RuntimeError("request budget exhausted")
        result["requests"] += 1
        return get_json(path, parameters)

    def gap(uid, low, high, reason):
        result["collectionComplete"] = False
        result["gaps"].append({"uid": uid, "startNs": str(low), "endExclusiveNs": str(high), "reason": reason})

    try:
        sources = request("/api/datasources", {})
        if not isinstance(sources, list):
            raise ValueError("Invalid datasource discovery")
    except Exception as error:
        gap(None, start, end, type(error).__name__)
        return result
    supported = 0
    for source in sources:
        if not isinstance(source, dict):
            gap(None, start, end, "invalid-datasource-identity")
            continue
        uid, kind = source.get("uid"), source.get("type")
        if kind not in ("loki", "prometheus"):
            continue
        if not isinstance(uid, str) or not re.fullmatch(r"[A-Za-z0-9_-]{1,80}", uid):
            gap(None, start, end, "invalid-datasource-identity")
            continue
        supported += 1
        prefix = f"/api/datasources/proxy/uid/{uid}"
        collected = {"uid": uid, "type": kind}
        result["sources"].append(collected)
        if kind == "loki":
            collected.update(slices=[], counts={}, rowCountLowerBound=0)
            counts = Counter()

            def window(low, high):
                coverage = {"startNs": str(low), "endExclusiveNs": str(high), "limit": limit}
                collected["slices"].append(coverage)
                try:
                    response = request(prefix + "/loki/api/v1/query_range", {
                        "query": selector, "start": str(low), "end": str(high),
                        "limit": str(limit), "direction": "forward"})
                    if response.get("status") != "success" or response["data"].get("resultType") != "streams":
                        raise ValueError("Invalid Loki response")
                    rows = []
                    raw_count = 0
                    for stream in response["data"]["result"]:
                        labels = stream["stream"]
                        service = labels.get("service_name", labels.get("app", labels.get("job", "other")))
                        service = service if service in services else "other"
                        for timestamp, line, *_ in stream["values"]:
                            if not isinstance(line, str):
                                raise ValueError("Invalid Loki log line")
                            raw_count += 1
                            if low <= int(timestamp) < high:
                                rows.append((service, line))
                    coverage.update(returnedCount=raw_count, saturated=raw_count >= limit)
                    if raw_count >= limit and high - low > 1:
                        coverage["split"] = True
                        midpoint = (low + high) // 2
                        window(low, midpoint)
                        window(midpoint, high)
                        return
                    if raw_count >= limit:
                        gap(uid, low, high, "saturated-single-timestamp")
                    for service, line in rows:
                        level = "unknown"
                        try:
                            payload = json.loads(line)
                            raw_level = str(payload.get("Level", payload.get("level", "unknown"))).lower()
                            aliases = {"warn": "warning", "information": "info", "fatal": "critical"}
                            raw_level = aliases.get(raw_level, raw_level)
                            if raw_level in ("trace", "debug", "info", "warning", "error", "critical"):
                                level = raw_level
                        except (ValueError, AttributeError):
                            pass
                        counts[(service, level)] += 1
                    collected["rowCountLowerBound"] += len(rows)
                except Exception as error:
                    coverage["errorClass"] = type(error).__name__
                    gap(uid, low, high, type(error).__name__)

            width = int(slice_seconds * 1_000_000_000)
            for low in range(start, end, width):
                window(low, min(end, low + width))
            collected["counts"] = [{"service": service, "level": level, "count": count}
                                   for (service, level), count in sorted(counts.items())]
        else:
            collected.update(observationKind="instant-snapshot", atUtc=to_utc, metrics=[])
            for name, expression in (metrics or {}).items():
                try:
                    response = request(prefix + "/api/v1/query", {"query": expression, "time": str(end / 1_000_000_000)})
                    if response.get("status") != "success" or response["data"].get("resultType") != "vector":
                        raise ValueError("Invalid Prometheus response")
                    values = [float(item["value"][1]) for item in response["data"]["result"]]
                    finite = [value for value in values if math.isfinite(value)]
                    collected["metrics"].append({"name": name, "sampleCount": len(values), "finiteSampleCount": len(finite),
                                                 "minimum": min(finite) if finite else None,
                                                 "maximum": max(finite) if finite else None})
                    if len(finite) != len(values):
                        gap(uid, end, end, "non-finite-metric-sample")
                except Exception as error:
                    gap(uid, end, end, type(error).__name__)
    if not supported:
        gap(None, start, end, "no-supported-datasource")
    return result
