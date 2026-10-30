"""Cover audit completeness, advisory grouping and the actual runner contract."""

from contextlib import redirect_stdout
from copy import deepcopy
import io
import json
import os
from pathlib import Path
import subprocess
import sys
import tempfile
import unittest
from unittest.mock import Mock

import audit_backend_dependencies as audit


class BackendDependencyAuditTests(unittest.TestCase):
    def setUp(self):
        self.temporary = tempfile.TemporaryDirectory()
        self.addCleanup(self.temporary.cleanup)
        self.root = Path(self.temporary.name)
        (self.root / "FoodDiary.slnx").write_text(
            '<Solution><Project Path="Api/Api.csproj" /><Project Path="Worker/Worker.csproj" /></Solution>',
            encoding="utf-8",
        )
        self.expected = audit.solution_projects(self.root)
        self.report = {
            "version": 1, "parameters": "--vulnerable --include-transitive",
            "sources": ["https://api.nuget.org/v3/index.json"],
            "projects": [
                {"path": str(self.root / name), "frameworks": [
                    {"framework": "net10.0", "topLevelPackages": []},
                ]} for name in ("Api/Api.csproj", "Worker/Worker.csproj")
            ],
        }

    def package(self, *, severity="High", version="1.0.0", advisory="GHSA-test"):
        return {
            "id": "Example.Package", "resolvedVersion": version,
            "vulnerabilities": [{"severity": severity, "advisoryurl": f"https://github.com/advisories/{advisory}"}],
        }

    def parse(self, report=None):
        return audit.parse_report(json.dumps(self.report if report is None else report), self.root, self.expected)

    def execute(self, report=None, results=None):
        runner = Mock(side_effect=results or [
            subprocess.CompletedProcess([], 0, "Restore completed.", ""),
            subprocess.CompletedProcess([], 0, json.dumps(self.report if report is None else report), ""),
        ])
        output = self.root / "logs"
        summary = self.root / "summary.md"
        console = io.StringIO()
        with redirect_stdout(console):
            status = audit.run_audit(self.root, output, summary, runner=runner)
        return status, runner, output, summary.read_text(encoding="utf-8"), console.getvalue()

    def test_clean_report_has_one_short_success_sentence_without_project_table(self):
        status, runner, output, summary, console = self.execute()
        self.assertEqual(0, status)
        self.assertIn("2 projects", summary)
        self.assertEqual(1, summary.count("✅"))
        self.assertNotIn("|", summary)
        self.assertNotIn("Api.csproj", summary)
        self.assertEqual(summary.strip(), console.strip())
        self.assertEqual("", (output / "failure-meta.env").read_text(encoding="utf-8"))
        commands = [call.args[0] for call in runner.call_args_list]
        self.assertEqual(["dotnet", "restore", "FoodDiary.slnx", "--locked-mode", "-p:NuGetAudit=false"], commands[0])
        self.assertEqual(["dotnet", "package", "list", "--project", "FoodDiary.slnx", "--vulnerable",
                          "--include-transitive", "--no-restore", "--format", "json", "--output-version", "1"], commands[1])
        self.assertEqual(2, len(commands), "Exactly one restore and one vulnerability audit")
        self.assertEqual(self.root, runner.call_args_list[1].kwargs["cwd"])
        self.assertEqual(600, runner.call_args_list[1].kwargs["timeout"])

    def test_same_advisory_is_grouped_across_projects_frameworks_and_dependency_kinds(self):
        for project in self.report["projects"]:
            project["frameworks"][0]["transitivePackages"] = [self.package()]
        self.report["projects"][0]["frameworks"].append({
            "framework": "net9.0", "topLevelPackages": [self.package(severity="Critical")],
        })
        findings = self.parse()
        self.assertEqual(1, len(findings))
        self.assertEqual("critical", findings[0].severity)
        self.assertEqual(3, len(findings[0].locations))
        status, _, output, summary, _ = self.execute()
        self.assertEqual(1, status)
        self.assertEqual(1, summary.count("| Example.Package |"))
        self.assertIn("Api/Api.csproj (net9.0, direct)", summary)
        self.assertIn("Worker/Worker.csproj (net10.0, transitive)", summary)
        self.assertIn("Critical", summary)
        metadata = (output / "failure-meta.env").read_text(encoding="utf-8")
        self.assertIn("FAILED_AREA=dotnet", metadata)
        self.assertIn("FAILED_TARGET=Vulnerable backend dependencies", metadata)
        self.assertIn("FAILED_LOG_FILE_NAME=audit.log", metadata)
        self.assertEqual(summary, (output / "audit.log").read_text(encoding="utf-8"))
        self.assertEqual(self.report, json.loads((output / "audit.json").read_text(encoding="utf-8")))

    def test_distinct_versions_and_advisories_are_not_merged(self):
        self.report["projects"][0]["frameworks"][0]["topLevelPackages"] = [
            self.package(), self.package(version="2.0.0"), self.package(advisory="GHSA-other"),
        ]
        self.assertEqual(3, len(self.parse()))

    def test_low_severity_transitive_advisory_still_fails(self):
        self.report["projects"][0]["frameworks"][0]["transitivePackages"] = [self.package(severity="Low")]
        self.assertEqual(1, self.execute()[0])

    def test_incomplete_or_untrusted_report_cannot_pass(self):
        variants = {}
        for label in ("no projects", "missing project", "duplicate project", "unknown project",
                      "no frameworks", "no package analysis", "missing source", "wrong parameters",
                      "unsupported schema", "source warning", "invalid packages"):
            variants[label] = deepcopy(self.report)
        variants["no projects"]["projects"] = []
        variants["missing project"]["projects"].pop()
        variants["duplicate project"]["projects"][1] = deepcopy(self.report["projects"][0])
        variants["unknown project"]["projects"][1]["path"] = str(self.root / "Other.csproj")
        variants["no frameworks"]["projects"][0]["frameworks"] = []
        del variants["no package analysis"]["projects"][0]["frameworks"][0]["topLevelPackages"]
        variants["missing source"]["sources"] = []
        variants["wrong parameters"]["parameters"] = "--include-transitive"
        variants["unsupported schema"]["version"] = True
        variants["source warning"]["problems"] = [{"level": "warning", "text": "NU1900: feed unavailable"}]
        variants["invalid packages"]["projects"][0]["frameworks"][0]["topLevelPackages"] = {}
        for label, report in variants.items():
            with self.subTest(label=label):
                with self.assertRaises(ValueError):
                    self.parse(report)
                status, _, output, summary, _ = self.execute(report=report)
                self.assertEqual(1, status)
                self.assertNotIn("✅", summary)
                self.assertIn("Audit did not complete", summary)
                self.assertIn("FAILED_LOG_FILE_NAME=audit.log", (output / "failure-meta.env").read_text(encoding="utf-8"))

    def test_missing_advisory_details_and_unknown_severity_fail(self):
        for changes in ({"vulnerabilities": []}, {"resolvedVersion": None},
                        {"vulnerabilities": [{"severity": "Unknown", "advisoryurl": "https://example.test"}]},
                        {"vulnerabilities": [{"severity": "High", "advisoryurl": "javascript:alert(1)"}]}):
            report = deepcopy(self.report)
            report["projects"][0]["frameworks"][0]["topLevelPackages"] = [self.package() | changes]
            with self.subTest(changes=changes), self.assertRaises(ValueError):
                self.parse(report)

    def test_restore_failure_stops_before_audit_and_retains_restore_log(self):
        status, runner, output, summary, _ = self.execute(results=[
            subprocess.CompletedProcess([], 7, "Partial restore", "Feed unavailable"),
        ])
        self.assertEqual(1, status)
        self.assertEqual(1, runner.call_count)
        self.assertIn("exit code 7", summary)
        self.assertIn("Feed unavailable", (output / "restore.log").read_text(encoding="utf-8"))
        self.assertIn("FAILED_LOG_FILE_NAME=restore.log", (output / "failure-meta.env").read_text(encoding="utf-8"))

    def test_command_failure_malformed_json_and_stderr_diagnostics_fail(self):
        for response in (subprocess.CompletedProcess([], 1, "{}", "SDK failed"),
                         subprocess.CompletedProcess([], 0, "", ""),
                         subprocess.CompletedProcess([], 0, "not json", ""),
                         subprocess.CompletedProcess([], 0, json.dumps(self.report), "NU1905: audit unavailable")):
            with self.subTest(response=response):
                status, _, _, summary, _ = self.execute(results=[
                    subprocess.CompletedProcess([], 0, "", ""), response,
                ])
                self.assertEqual(1, status)
                self.assertNotIn("✅", summary)

    def test_timeout_is_bounded_and_keeps_partial_diagnostics(self):
        status, _, output, summary, _ = self.execute(results=[
            subprocess.CompletedProcess([], 0, "", ""),
            subprocess.TimeoutExpired("dotnet", 600, output=b"partial output", stderr=b"partial error"),
        ])
        self.assertEqual(1, status)
        self.assertIn("600-second timeout", summary)
        self.assertIn("partial output", (output / "audit.log").read_text(encoding="utf-8"))

    def test_missing_dotnet_executable_is_a_failure_with_diagnostics(self):
        status, _, output, summary, _ = self.execute(results=[FileNotFoundError("dotnet unavailable")])
        self.assertEqual(1, status)
        self.assertIn("dotnet unavailable", summary)
        self.assertIn("FAILED_LOG_FILE_NAME=restore.log", (output / "failure-meta.env").read_text(encoding="utf-8"))

    def test_summary_escapes_untrusted_report_text(self):
        finding = audit.Finding("Bad|<script>", "1.0", "https://example.test/advisory/(test)", "high", {"Api\n|<b>"})
        summary = audit.render_summary(1, [finding])
        self.assertNotIn("<script>", summary)
        self.assertNotIn("<b>", summary)
        self.assertIn("\\|", summary)
        self.assertIn("%28test%29", summary)

    def test_cli_uses_utf8_even_when_windows_inherits_an_ansi_codepage(self):
        script = (
            "import audit_backend_dependencies as audit; "
            "audit.run_audit = lambda *args: (print('✅ Русский текст'), 0)[1]; "
            "raise SystemExit(audit.main())"
        )
        result = subprocess.run(
            [sys.executable, "-B", "-c", script], cwd=Path(__file__).parent,
            capture_output=True, text=True, encoding="utf-8",
            env=os.environ | {"PYTHONIOENCODING": "cp1252"},
        )
        self.assertEqual(0, result.returncode, result.stderr)
        self.assertIn("✅ Русский текст", result.stdout)


if __name__ == "__main__":
    unittest.main()
