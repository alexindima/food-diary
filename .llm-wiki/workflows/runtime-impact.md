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

The refreshed topology includes the shared local ranking reader without introducing a host, worker, provider, or recurring job. Graph snapshot freshness, projection generation, and full verification remain independent evidence when interpreting this runtime inventory.

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

SQLite is the only compiled-index query provider. Generated JSON snapshots remain
inputs for the sole Node projection writer and reviewable Git artifacts. Queries
validate exact source hashes, select bounded records in SQL, and report explicit
recovery errors when preparation fails. Backend-only preparation works without
TypeScript; frontend code-graph discovery requires the locked npm dependencies.
Direct behavior tests cover identity, selection, scope, freshness and output bounds.
