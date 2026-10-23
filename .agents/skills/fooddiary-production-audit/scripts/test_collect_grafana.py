import json
import unittest
from collect_grafana import collect_audit, utc_nanoseconds

START = "2026-10-23T00:00:00Z"
END = "2026-10-23T00:00:01Z"


class CollectionTests(unittest.TestCase):
    def test_saturated_slices_preserve_repeated_rows_without_parent_double_count(self):
        low = utc_nanoseconds(START)
        rows = [(low, '{"Level":"Error","message":"private@example.org password=secret"}'),
                (low, '{"Level":"Error","message":"private@example.org password=secret"}'),
                (low + 500000000, '{"level":"info"}')]

        def get(path, params):
            if path == "/api/datasources":
                return [{"uid": "logs", "type": "loki"}]
            selected = [[str(timestamp), line] for timestamp, line in rows
                        if int(params["start"]) <= timestamp < int(params["end"])][:int(params["limit"])]
            return {"status": "success", "data": {"resultType": "streams", "result": [
                {"stream": {"service_name": "api", "private_label": "secret"}, "values": selected}]}}

        result = collect_audit(get, START, END, selector='{service_name="api"}', service_names=("api",), limit=3)
        self.assertTrue(result["collectionComplete"])
        self.assertEqual(result["sources"][0]["rowCountLowerBound"], 3)
        self.assertIn({"service": "api", "level": "error", "count": 2}, result["sources"][0]["counts"])
        self.assertNotIn("private@example.org", json.dumps(result))
        self.assertNotIn("secret", json.dumps(result))
        self.assertNotIn("service_name=", json.dumps(result))

    def test_single_timestamp_saturation_and_request_budget_remain_incomplete(self):
        def get(path, params):
            if path == "/api/datasources":
                return [{"uid": "logs", "type": "loki"}]
            return {"status": "success", "data": {"resultType": "streams", "result": [
                {"stream": {}, "values": [[params["start"], "private"], [params["start"], "private"]]}]}}

        result = collect_audit(get, START, END, selector='{job=~".+"}', limit=2, max_requests=4)
        self.assertFalse(result["collectionComplete"])
        self.assertEqual(result["requests"], 4)
        self.assertTrue(result["gaps"])
        self.assertNotIn("private", json.dumps(result))

    def test_unavailable_discovery_does_not_claim_no_events_or_leak_error(self):
        def get(path, params):
            raise RuntimeError("Authorization: Bearer secret")
        result = collect_audit(get, START, END, selector='{job=~".+"}')
        self.assertFalse(result["collectionComplete"])
        self.assertEqual(result["sources"], [])
        self.assertNotIn("secret", json.dumps(result))

    def test_prometheus_values_are_explicitly_snapshots_not_window_health(self):
        def get(path, params):
            if path == "/api/datasources":
                return [{"uid": "metrics", "type": "prometheus"}]
            return {"status": "success", "data": {"resultType": "vector", "result": [
                {"metric": {"private": "secret"}, "value": [0, "1"]}]}}
        result = collect_audit(get, START, END, selector='{job=~".+"}', metrics={"targets": "up"})
        source = result["sources"][0]
        self.assertEqual(source["observationKind"], "instant-snapshot")
        self.assertEqual(source["metrics"][0]["minimum"], 1)
        self.assertTrue(result["uncoveredEvidence"])
        self.assertNotIn("secret", json.dumps(result))

    def test_offsets_are_exact_and_naive_timestamps_are_rejected(self):
        self.assertEqual(utc_nanoseconds(START), utc_nanoseconds("2026-10-23T04:00:00+04:00"))
        with self.assertRaises(ValueError):
            utc_nanoseconds("2026-10-23T00:00:00")

    def test_malformed_sources_and_lines_are_incomplete_without_partial_counts(self):
        def get(path, params):
            if path == "/api/datasources":
                return [None, {"uid": "logs", "type": "loki"}]
            return {"status": "success", "data": {"resultType": "streams", "result": [
                {"stream": {}, "values": [[params["start"], "valid"], [params["start"], None]]}]}}
        result = collect_audit(get, START, END, selector='{job=~".+"}')
        self.assertFalse(result["collectionComplete"])
        self.assertEqual(result["sources"][0]["rowCountLowerBound"], 0)
        self.assertEqual(result["sources"][0]["counts"], [])
        self.assertEqual(result["revisionStatus"], "unknown")

    def test_single_nanosecond_saturation_is_explicit(self):
        def get(path, params):
            if path == "/api/datasources":
                return [{"uid": "logs", "type": "loki"}]
            values = [[params["start"], "private"]] if params["start"] == str(utc_nanoseconds(START)) else []
            return {"status": "success", "data": {"resultType": "streams", "result": [
                {"stream": {}, "values": values}]}}
        result = collect_audit(get, START, END, selector='{job=~".+"}', limit=1)
        self.assertFalse(result["collectionComplete"])
        self.assertTrue(any(gap["reason"] == "saturated-single-timestamp" for gap in result["gaps"]))
        self.assertEqual(result["sources"][0]["rowCountLowerBound"], 1)
        self.assertLess(result["requests"], 100)

    def test_invalid_bounds_and_revision_are_rejected_before_transport(self):
        def get(path, params):
            self.fail("Invalid inputs must not make requests")
        for kwargs in ({"slice_seconds": 1e-12}, {"max_requests": 1.5}, {"limit": True}, {"deployed_revision": "main"}):
            with self.subTest(kwargs=kwargs), self.assertRaises(ValueError):
                collect_audit(get, START, END, selector='{job=~".+"}', **kwargs)


if __name__ == "__main__":
    unittest.main()
