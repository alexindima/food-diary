---
id: workflow-runtime-impact
kind: workflow
status: current
title: Review runtime and integration impact
summary: Find deployable services, workers, external clients, webhooks, and recurring jobs related to a change.
tags:
  - workflow
  - runtime
  - integrations
  - resilience
sources:
  - .llm-wiki/generated/runtime-topology.json
  - .llm-wiki/tools/Find-LlmWikiRuntimeTopology.ps1
  - .llm-wiki/tools/Measure-LlmWikiStandaloneIndexRoutes.ps1
  - .llm-wiki/tools/Get-LlmWikiCompiledIndexMigration.ps1
  - .llm-wiki/policies/change-policies.json
---

# Review runtime and integration impact

```powershell
./.llm-wiki/wiki.ps1 topology
./.llm-wiki/wiki.ps1 topology -Query MailRelay
```

For matched clients, workers, jobs, and webhooks, review cancellation, timeout,
retry/backoff, idempotency/replay, duplicate delivery, ordering, partial failure,
dead-letter/recovery, shutdown behavior, health/readiness, and telemetry.

Task-brief impact selection uses the SQLite projection, but this small standalone
query intentionally keeps the generated JSON route. The measured Node/SQLite
process boundary costs more than parsing the 16 KiB source in the current
PowerShell process. Re-run `Measure-LlmWikiStandaloneIndexRoutes.ps1` after an
in-process or persistent SQLite reader exists; switch only after exact output
parity and a non-regressing end-to-end measurement both pass.
