import json
from pathlib import Path
import tempfile
import unittest

from run_audit import render_report, run_audit


class AuditRunnerTests(unittest.TestCase):
    def test_fixed_window_and_totals_come_from_the_saved_run_and_aggregates(self):
        calls = []

        def get(path, params):
            calls.append((path, params))
            if path == "/api/datasources":
                return [{"uid": "logs", "type": "loki"}]
            return {"status": "success", "data": {"resultType": "streams", "result": [
                {"stream": {"service_name": "api"}, "values": [
                    [params["start"], '{"level":"error","message":"private@example.org token=secret"}'],
                    [params["start"], '{"level":"info"}']]}]}}

        with tempfile.TemporaryDirectory() as directory:
            path = Path(directory) / "run.json"
            original = {"id": "12345678-1234-1234-1234-123456789012",
                        "fromUtc": "2026-11-05T00:00:00Z", "toUtc": "2026-11-05T00:00:01Z",
                        "private": "secret"}
            path.write_text(json.dumps(original), encoding="utf-8")
            result = run_audit(get, path, {"selector": '{service_name="api"}', "service_names": ["api"]})
            self.assertEqual(json.loads(path.read_text()), original)
        self.assertEqual(result["collection"]["fromUtc"], original["fromUtc"])
        self.assertEqual(result["collection"]["toUtc"], original["toUtc"])
        self.assertIn("observed rows (lower bound): 2", result["report"])
        self.assertIn("| api | 0 | 0 | 1 | 0 | 1 | 0 | 0 | 2 |", result["report"])
        self.assertIn("2026-11-05T04:00:00+04:00", result["report"])
        self.assertNotIn("secret", json.dumps(result))
        self.assertNotIn("private@example.org", json.dumps(result))
        self.assertEqual(len(calls), 2)

    def test_incomplete_collection_and_absent_sources_do_not_become_healthy(self):
        report = render_report({"fromUtc": "2026-11-05T00:00:00Z", "toUtc": "2026-11-05T01:00:00Z",
                                "collectionComplete": False, "sources": [], "gaps": [{}]})
        self.assertIn("complete: no", report)
        self.assertIn("Loki sources: 0", report)
        self.assertIn("Collection gaps: 1", report)

    def test_invalid_rows_cannot_inject_report_content_or_counts(self):
        collection = {"fromUtc": "2026-11-05T00:00:00Z", "toUtc": "2026-11-05T01:00:00Z",
                      "collectionComplete": True, "gaps": [], "sources": [{"type": "loki", "counts": []}]}
        for row in [{"service": "api\nprivate", "level": "error", "count": 1},
                    {"service": "api", "level": "error", "count": -1},
                    {"service": "api", "level": "error", "count": True}]:
            collection["sources"][0]["counts"] = [row]
            with self.assertRaises(ValueError):
                render_report(collection)


if __name__ == "__main__":
    unittest.main()
