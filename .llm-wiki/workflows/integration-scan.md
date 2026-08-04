---
id: workflow-integration-scan
kind: workflow
status: current
title: Scan integration edges for boundary-bearing work
summary: Compose existing contract, ownership, runtime, side-effect, external-boundary, and verification evidence without creating a second source of truth.
tags:
  - workflow
  - integrations
  - contracts
  - runtime
sources:
  - .llm-wiki/tools/Get-LlmWikiIntegrationScan.ps1
  - .llm-wiki/tools/Test-LlmWikiIntegrationScan.ps1
  - .llm-wiki/tools/Get-LlmWikiTaskBrief.ps1
  - .llm-wiki/tools/Get-LlmWikiResearchPacket.ps1
---

# Integration scan

Use the scanner for cross-layer, API, provider, external-data, asynchronous,
`critical`, or `architectural` work:

```powershell
./.llm-wiki/wiki.ps1 integration-scan `
  -Intent '<task>' `
  -PlannedPath 'path/one','path/two'
```

The command composes existing Wiki evidence into one bounded view:

- inbound backend and frontend consumers;
- outbound API, DI, and module dependencies;
- webhooks, recurring jobs, hosted services, and logging side effects;
- asynchronous continuations;
- HTTP, webhook, provider, and external-data boundaries;
- focused tests, risk scenarios, required checks, and explicit evidence gaps.

The output is navigation evidence, not authority. Confirm every reported edge and
every empty category in current source. The scanner does not write a new index or
persist agent scratch output.

The command is opt-in. Its `recommended` flag is false for bounded `tiny`, visual
UI, maintenance, and local bug work without contract, runtime, external, or
cross-layer evidence; those routes gain no mandatory stage.
