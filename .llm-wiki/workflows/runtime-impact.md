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

Every inferred `behaviorSignals` list identifies its `behaviorSignalScope`.
Class and registration windows prevent one type or recurring-job registration
from inheriting retry, cancellation, outbox, or concurrency words from the whole
file. Recurring-job records intentionally leave target behavior unexpanded and
say to inspect the registered job implementation. Cancellation distinguishes a
propagation candidate from explicit `CancellationToken.None`; webhook replay or
duplicate controls are searchable as idempotency review candidates without
claiming that end-to-end idempotency was proved.

Task-brief impact selection and the standalone command use the exact SQLite
projection. The generated JSON is only an explicit parity oracle and projection
source, not an automatic fallback. Runtime accepts the measured fresh-process
reader-load cost in exchange for one production query mechanism; the loaded
reader is cached across tool-script scopes. Telemetry separates fresh-process
p50/p95 from warmed p50/p95 so that tradeoff cannot be hidden by a single average.
