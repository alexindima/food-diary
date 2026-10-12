import datetime
import json
import tempfile
import unittest
import uuid
from pathlib import Path
from unittest.mock import patch
from concurrent.futures import ThreadPoolExecutor

import audit


class AuditBridgeTests(unittest.TestCase):
    def test_competing_prepared_results_never_overwrite_the_winner(self):
        with tempfile.TemporaryDirectory() as temp:
            path = Path(temp) / "completion.json"
            candidates = [{"summary": str(index)} for index in range(4)]
            def attempt(value):
                try:
                    audit.save_completion(path, value)
                    return value
                except ValueError:
                    return None
            with ThreadPoolExecutor(max_workers=4) as pool:
                winners = [result for result in pool.map(attempt, candidates) if result is not None]
            self.assertEqual(1, len(winners))
            self.assertEqual(winners[0], json.loads(path.read_text()))
            self.assertEqual([path], list(Path(temp).iterdir()))

    def test_start_persists_id_and_fixed_utc_window_before_network_failure(self):
        now = datetime.datetime(2026, 10, 12, 9, tzinfo=datetime.timezone(datetime.timedelta(hours=4)))
        with tempfile.TemporaryDirectory() as temp, patch.object(audit, "call_api", side_effect=OSError):
            with self.assertRaises(OSError):
                audit.start({}, Path(temp), now)
            run = json.loads(next(Path(temp).glob("*/run.json")).read_text())
            self.assertEqual("2026-10-12T05:00:00+00:00", run["toUtc"])
            self.assertEqual("2026-10-11T05:00:00+00:00", run["fromUtc"])
            self.assertEqual(str(uuid.UUID(run["id"])), run["id"])

    def test_lost_completion_response_retries_same_ids_and_payload_without_reinvestigation(self):
        with tempfile.TemporaryDirectory() as temp, patch.object(audit, "call_api") as request:
            path = audit.start({}, Path(temp))
            result = {"outcome": "findings", "summary": "One confirmed defect", "findings": [
                {"title": "Broken navigation", "outcome": "draft_ready", "summary": "Regression verified",
                 "mergeRequestUrl": "https://github.com/example/repo/pull/1"}]}
            request.side_effect = [None, OSError()]
            with self.assertRaises(OSError):
                audit.complete({}, path, result)
            first_payload = request.call_args.args[3]
            self.assertTrue((path.parent / "completion.json").exists())
            self.assertFalse((path.parent / "published.json").exists())
            request.side_effect = None
            audit.retry_pending({}, Path(temp))
            self.assertEqual(first_payload, request.call_args.args[3])
            self.assertTrue((path.parent / "published.json").exists())
            count = request.call_count
            audit.complete({}, path, result)
            self.assertEqual(count, request.call_count)
            with self.assertRaises(ValueError):
                audit.complete({}, path, {**result, "summary": "Conflicting retry"})

    def test_incomplete_audit_can_be_published_without_claiming_a_clean_result(self):
        run = {"id": str(uuid.uuid4())}
        result = audit.build_completion(run, {"outcome": "incomplete", "summary": "Loki unavailable", "findings": []})
        self.assertEqual("incomplete", result["outcome"])

    def test_expired_local_results_cannot_be_republished(self):
        now = datetime.datetime(2026, 10, 12, 5, tzinfo=datetime.timezone.utc)
        with tempfile.TemporaryDirectory() as temp, patch.object(audit, "call_api"):
            path = audit.start({"auditRetentionDays": 1}, Path(temp), now)
            self.assertEqual("2026-10-13T05:00:00+00:00", json.loads(path.read_text())["expiresAtUtc"])
            with self.assertRaises(ValueError):
                audit.load_run(path, now + datetime.timedelta(days=1))

    def test_invalid_outcomes_duplicate_pr_credentials_and_unbounded_results_are_rejected(self):
        run = {"id": str(uuid.uuid4())}
        finding = {"title": "Defect", "outcome": "draft_ready", "summary": "Evidence", "mergeRequestUrl": None}
        cases = [
            {"outcome": "findings", "summary": "No findings", "findings": []},
            {"outcome": "clean", "summary": "Contradiction", "findings": [finding]},
            {"outcome": "findings", "summary": "Missing URL", "findings": [finding]},
            {"outcome": "findings", "summary": "Credentials", "findings": [
                {**finding, "mergeRequestUrl": "https://secret@example.test/pull/1"}]},
            {"outcome": "incomplete", "summary": "Bound", "findings": [finding] * 21},
        ]
        for case in cases:
            with self.subTest(case=case["summary"]), self.assertRaises(ValueError):
                audit.build_completion(run, case)


if __name__ == "__main__":
    unittest.main()
