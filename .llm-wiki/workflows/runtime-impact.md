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

After a failed EF transaction, do not assume rollback restored the `DbContext`
to a safe reusable state. For recognized `DbUpdateException` and unique-
constraint races, inspect tracked entity states and verify that rejected changes
are cleared or recreated before a retry or follow-up transaction. When inbox or
outbox completion is persisted after such a rollback, require provider-backed
coverage proving that only the intended completion state reaches the database.

Task-brief impact selection and the standalone command normally use the exact
SQLite projection. On a cold checkout without TypeScript prerequisites, the
read-only facade automatically uses the committed JSON baseline unless SQLite
was explicitly required. Runtime accepts the measured fresh-process
reader-load cost in exchange for one production query mechanism; the loaded
reader is cached across tool-script scopes. Telemetry separates fresh-process
p50/p95 from warmed p50/p95 so that tradeoff cannot be hidden by a single average.
