"""Run one complete NuGet vulnerability audit and publish its CI summary."""

import argparse
from dataclasses import dataclass, field
from html import escape
import json
import os
from pathlib import Path
import shlex
import subprocess
import sys
import xml.etree.ElementTree as ET
from urllib.parse import quote, urlsplit


SEVERITIES = {"low": 0, "moderate": 1, "high": 2, "critical": 3}


@dataclass
class Finding:
    package: str
    version: str
    advisory: str
    severity: str
    locations: set[str] = field(default_factory=set)


def project_key(value: str, root: Path) -> str:
    return os.path.normcase(os.path.abspath(root / value.replace("\\", "/")))


def solution_projects(root: Path) -> set[str]:
    paths = [text(item.attrib.get("Path"), "solution project path") for item in ET.parse(root / "FoodDiary.slnx").iter("Project")]
    projects = {project_key(path, root) for path in paths}
    if not projects or len(projects) != len(paths):
        raise ValueError("Solution must contain a nonempty, unique project inventory.")
    return projects


def text(value, label: str) -> str:
    if not isinstance(value, str) or not value.strip():
        raise ValueError(f"Missing or invalid {label}.")
    return value.strip()


def problems(value):
    if isinstance(value, dict):
        for key, child in value.items():
            if key in ("problems", "errors", "warnings") and child:
                yield json.dumps(child, ensure_ascii=False)
            else:
                yield from problems(child)
    elif isinstance(value, list):
        for child in value:
            yield from problems(child)


def parse_report(raw: str, root: Path, expected: set[str]) -> list[Finding]:
    report = json.loads(raw)
    if not isinstance(report, dict) or type(report.get("version")) is not int or report["version"] != 1:
        raise ValueError("Unsupported NuGet audit report schema.")
    parameters = shlex.split(text(report.get("parameters"), "audit parameters"))
    if not {"--vulnerable", "--include-transitive"}.issubset(parameters):
        raise ValueError("Report must include vulnerability and transitive dependency checks.")
    sources = report.get("sources")
    if not isinstance(sources, list) or not sources or not all(isinstance(source, str) and source.strip() for source in sources):
        raise ValueError("No vulnerability data sources were reported.")
    diagnostics = list(problems(report))
    if diagnostics:
        raise ValueError("NuGet audit reported problems: " + "; ".join(diagnostics))
    projects = report.get("projects")
    if not isinstance(projects, list) or not projects:
        raise ValueError("NuGet audit returned no project results.")
    actual = []
    findings = {}
    for project in projects:
        if not isinstance(project, dict):
            raise ValueError("Invalid project result.")
        path = text(project.get("path"), "project path")
        key = project_key(path, root)
        actual.append(key)
        location = os.path.relpath(os.path.abspath(root / path.replace("\\", "/")), root).replace("\\", "/")
        frameworks = project.get("frameworks")
        if not isinstance(frameworks, list) or not frameworks:
            raise ValueError(f"No framework results for {location}.")
        for framework in frameworks:
            if not isinstance(framework, dict):
                raise ValueError(f"Invalid framework result for {location}.")
            target = text(framework.get("framework"), "target framework")
            if not {"topLevelPackages", "transitivePackages"}.intersection(framework):
                raise ValueError(f"No package analysis for {location} ({target}).")
            for kind, label in (("topLevelPackages", "direct"), ("transitivePackages", "transitive")):
                packages = framework.get(kind, [])
                if not isinstance(packages, list):
                    raise ValueError(f"Invalid {kind} results for {location}.")
                for package in packages:
                    if not isinstance(package, dict):
                        raise ValueError("Invalid package result.")
                    name = text(package.get("id"), "package ID")
                    version = text(package.get("resolvedVersion"), "resolved package version")
                    advisories = package.get("vulnerabilities")
                    if not isinstance(advisories, list) or not advisories:
                        raise ValueError(f"No vulnerability details for listed package {name}.")
                    for advisory in advisories:
                        if not isinstance(advisory, dict):
                            raise ValueError(f"Invalid advisory for {name}.")
                        severity = text(advisory.get("severity"), "advisory severity").lower()
                        if severity not in SEVERITIES:
                            raise ValueError(f"Unknown advisory severity for {name}.")
                        url = text(advisory.get("advisoryurl"), "advisory URL")
                        parsed = urlsplit(url)
                        if parsed.scheme not in ("https", "http") or not parsed.netloc or any(char.isspace() for char in url):
                            raise ValueError(f"Invalid advisory URL for {name}.")
                        identity = (name.casefold(), version, url)
                        finding = findings.setdefault(identity, Finding(name, version, url, severity))
                        if SEVERITIES[severity] > SEVERITIES[finding.severity]:
                            finding.severity = severity
                        finding.locations.add(f"{location} ({target}, {label})")
    if len(actual) != len(set(actual)):
        raise ValueError("Duplicate project results in NuGet audit.")
    if set(actual) != expected:
        missing = sorted(os.path.relpath(path, root) for path in expected - set(actual))
        extra = sorted(os.path.relpath(path, root) for path in set(actual) - expected)
        raise ValueError(f"Incomplete solution audit. Missing projects: {missing}; unexpected projects: {extra}.")
    return sorted(findings.values(), key=lambda item: (-SEVERITIES[item.severity], item.package.casefold(), item.version, item.advisory))


def markdown(value: str) -> str:
    return escape(" ".join(value.split())).replace("|", "\\|").replace("`", "&#96;")


def render_summary(count: int, findings: list[Finding]) -> str:
    header = "## Backend dependency audit\n\n"
    if not findings:
        return header + f"✅ No vulnerable NuGet packages found in **{count} projects**, including transitive dependencies.\n"
    lines = [
        header + f"❌ Found **{len(findings)} package/version/advisory findings** across the solution.",
        "", "| Package | Version | Severity | Advisory | Affected projects |",
        "| --- | --- | --- | --- | --- |",
    ]
    for finding in findings:
        url = quote(finding.advisory, safe=":/?&=%#@+-,._~")
        advisory = markdown(urlsplit(finding.advisory).path.rstrip("/").split("/")[-1] or "Advisory")
        locations = "<br>".join(markdown(value) for value in sorted(finding.locations))
        lines.append(f"| {markdown(finding.package)} | {markdown(finding.version)} | {finding.severity.title()} | [{advisory}]({url}) | {locations} |")
    return "\n".join(lines) + "\n"


def run_audit(root: Path, output: Path, summary: Path | None = None, runner=None) -> int:
    runner = runner or subprocess.run
    output.mkdir(parents=True, exist_ok=True)
    for name in ("restore.log", "audit.log", "audit.json", "audit.stderr.log", "failure-meta.env"):
        (output / name).write_text("", encoding="utf-8")
    stage, log_name = "Restore dependency audit", "restore.log"
    findings = []
    try:
        expected = solution_projects(root)
        # This command only prepares locked assets. The mandatory JSON command below owns the single audit.
        commands = [
            ["dotnet", "restore", "FoodDiary.slnx", "--locked-mode", "-p:NuGetAudit=false"],
            ["dotnet", "package", "list", "--project", "FoodDiary.slnx", "--vulnerable",
             "--include-transitive", "--no-restore", "--format", "json", "--output-version", "1"],
        ]
        for index, command in enumerate(commands):
            if index:
                stage, log_name = "Audit backend solution dependencies", "audit.log"
            result = runner(command, cwd=root, capture_output=True, text=True, encoding="utf-8", errors="replace", timeout=600)
            (output / log_name).write_text(result.stdout + result.stderr, encoding="utf-8")
            if index:
                (output / "audit.json").write_text(result.stdout, encoding="utf-8")
                (output / "audit.stderr.log").write_text(result.stderr, encoding="utf-8")
            if result.returncode:
                raise ValueError(f"{stage} failed with exit code {result.returncode}.")
            if index and result.stderr.strip():
                raise ValueError("NuGet audit emitted diagnostics: " + result.stderr.strip())
        findings = parse_report(result.stdout, root, expected)
        body = render_summary(len(expected), findings)
        (output / "audit.log").write_text(body, encoding="utf-8")
    except (ValueError, OSError, ET.ParseError, subprocess.SubprocessError) as error:
        details = str(error)
        if isinstance(error, subprocess.TimeoutExpired):
            details = f"{stage} exceeded the 600-second timeout."
            partial = [value.decode("utf-8", errors="replace") if isinstance(value, bytes) else value or ""
                       for value in (error.stdout, error.stderr)]
            (output / log_name).write_text("\n".join(partial), encoding="utf-8")
        previous = (output / log_name).read_text(encoding="utf-8")
        (output / log_name).write_text(f"Error: {details}\n{previous}", encoding="utf-8")
        body = f"## Backend dependency audit\n\n❌ Audit did not complete.\n\n{markdown(details)}\n\nDetails: `{log_name}` in the failure artifact.\n"
        failed = True
    else:
        failed = bool(findings)
        if failed:
            stage, log_name = "Vulnerable backend dependencies", "audit.log"
    if failed:
        (output / "failure-meta.env").write_text(
            f"FAILED_AREA=dotnet\nFAILED_TARGET={stage}\nFAILED_LOG_FILE_NAME={log_name}\n", encoding="utf-8")
    if summary:
        with summary.open("a", encoding="utf-8") as stream:
            stream.write(body)
    print(body.strip())
    return int(failed)


def main() -> int:
    sys.stdout.reconfigure(encoding="utf-8")
    sys.stderr.reconfigure(encoding="utf-8")
    root = Path(__file__).resolve().parents[2]
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--output-dir", type=Path, default=root / ".artifacts/backend-dependency-audit")
    parser.add_argument("--summary", type=Path)
    args = parser.parse_args()
    return run_audit(root, args.output_dir, args.summary)


if __name__ == "__main__":
    raise SystemExit(main())
