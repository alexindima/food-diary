import io
import json
from pathlib import Path
import subprocess
import unittest
from unittest.mock import Mock

import check_primary_baseline_readiness as readiness


class BaselineReadinessTests(unittest.TestCase):
    def invoke(self, history=None, failure=None):
        runner = Mock()
        if failure is not None:
            runner.side_effect = failure
        else:
            runner.return_value = subprocess.CompletedProcess(
                [], 0, json.dumps(history), ""
            )
        stdout, stderr = io.StringIO(), io.StringIO()
        status = readiness.main(runner, stdout, stderr)
        return status, stdout.getvalue(), stderr.getvalue(), runner

    def test_empty_database_and_empty_history_are_ready(self):
        status, stdout, stderr, _ = self.invoke([])
        self.assertEqual(status, 0)
        self.assertIn("permits baseline deployment", stdout)
        self.assertEqual(stderr, "")

    def test_legacy_history_requires_explicit_adoption(self):
        for history in [
            ["20261006124036_AllowLiveUsdaFoodReferences"],
            ["20261017003851_HardenTelegramAssertionRecovery"],
        ]:
            with self.subTest(history=history):
                status, stdout, stderr, runner = self.invoke(history)
                self.assertEqual(status, 1)
                self.assertEqual(stdout, "")
                self.assertIn("explicit baseline adoption", stderr)
                runner.assert_called_once()

    def test_adopted_history_and_later_migrations_are_ready(self):
        for history in [
            [readiness.BASELINE],
            [readiness.BASELINE, "20261020000000_FutureMigration"],
        ]:
            with self.subTest(history=history):
                self.assertEqual(self.invoke(history)[0], 0)

    def test_inserting_a_baseline_marker_does_not_adopt_legacy_history(self):
        history = [
            "20261017003851_HardenTelegramAssertionRecovery",
            readiness.BASELINE,
        ]
        self.assertEqual(self.invoke(history)[0], 1)

    def test_unknown_response_shapes_and_duplicate_ids_fail_closed(self):
        for response in [None, {}, [True], ["unexpected"], [readiness.BASELINE] * 2]:
            with self.subTest(response=response):
                self.assertEqual(self.invoke(response)[0], 2)

    def test_database_failures_and_timeouts_fail_closed_without_disclosing_errors(self):
        for failure in [
            OSError("private transport details"),
            subprocess.CalledProcessError(1, [], stderr="private transport details"),
            subprocess.TimeoutExpired([], 30),
        ]:
            with self.subTest(failure=type(failure).__name__):
                status, stdout, stderr, _ = self.invoke(failure=failure)
                self.assertEqual(status, 2)
                self.assertEqual(stdout, "")
                self.assertNotIn("private transport details", stderr)

    def test_non_json_output_fails_closed(self):
        runner = Mock(return_value=subprocess.CompletedProcess([], 0, "not JSON", ""))
        self.assertEqual(readiness.main(runner, io.StringIO(), io.StringIO()), 2)

    def test_query_is_read_only_and_uses_the_server_connection_without_exposing_credentials(self):
        _, _, _, runner = self.invoke([readiness.BASELINE])
        args, kwargs = runner.call_args
        self.assertEqual(args[0], readiness.HISTORY_COMMAND)
        self.assertTrue(kwargs["input"].startswith("BEGIN READ ONLY;"))
        self.assertTrue(kwargs["input"].rstrip().endswith("COMMIT;"))
        self.assertIn("history_exists", kwargs["input"])
        self.assertIn("ON_ERROR_STOP=1", args[0][-1])
        self.assertTrue(kwargs["check"])
        self.assertTrue(kwargs["capture_output"])
        self.assertEqual(kwargs["timeout"], 30)

    def test_baseline_id_names_the_actual_migration(self):
        repository = Path(__file__).resolve().parents[2]
        self.assertTrue((repository / "FoodDiary.Infrastructure/Migrations" / (readiness.BASELINE + ".cs")).is_file())


if __name__ == "__main__":
    unittest.main()
