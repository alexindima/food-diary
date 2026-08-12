---
id: workflow-review-report
kind: workflow
status: current
title: Publish a change review report
summary: Render the compiled change packet and release-readiness scorecard as deterministic Markdown or JSON for humans and CI.
tags:
  - workflow
  - review
  - ci
  - report
sources:
  - .llm-wiki/tools/LlmWikiChangePacket.ps1
  - .llm-wiki/tools/Get-LlmWikiReviewReport.ps1
  - .llm-wiki/tools/Get-LlmWikiReleaseReadiness.ps1
  - .llm-wiki/tools/Get-LlmWikiChangePacket.ps1
  - .github/workflows/ci-tests.yml
---

# Publish a Change Review Report

Generate a Markdown summary for a pull request, task handoff, or CI job:

```powershell
./.llm-wiki/wiki.ps1 report `
  -BaseRef origin/master `
  -HeadRef HEAD `
  -Objective "Describe the intended outcome" `
  -OutputPath .artifacts/llm-wiki/review-report.md
```

Use `-Format Json` for automation. The report includes a stable packet
fingerprint, change scope, risk, every readiness dimension, concrete findings,
required checks, review obligations, and suggested test scenarios.
Module values are normalized to stable names rather than serializing internal
objects, so malformed placeholders cannot enter the generated report.
Objective metadata uses the shared current/legacy packet reader, keeping reports
and governed delivery commands compatible with the same workspace formats.

CI deliberately produces the report without requiring local manifest,
acceptance, or evidence files. Missing optional governance artifacts therefore
appear as `conditional` instead of making an otherwise valid pull request fail.
Use the strict readiness command when those artifacts are part of the delivery
contract.

The dedicated CI Wiki job runs full verification alongside the backend,
PostgreSQL, dependency-audit, and frontend jobs. It starts with deterministic
lint and portable regressions, then runs index checks and the complete
stateful developer-tool smoke suite concurrently before publishing this report.
