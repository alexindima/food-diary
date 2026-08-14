# Backend Module Extraction Readiness

## Purpose

This document defines when a module may move from the primary modular monolith
to a separately deployed service and database. Logical ownership remains the
default; physical extraction requires an operational or organizational reason.

The executable source analysis remains authoritative:

```powershell
./.llm-wiki/wiki.ps1 extraction -Module <Module>
dotnet test tests/FoodDiary.ArchitectureTests/FoodDiary.ArchitectureTests.csproj
```

## Required exit criteria

A candidate is ready for an extraction design only when all applicable checks
are satisfied:

| Area | Required evidence |
| --- | --- |
| Ownership | The module is the only writer of its aggregates and tables. |
| Application API | Other modules use narrow commands, capabilities, events, or projection contracts; no foreign repository access. |
| Dependency graph | No undeclared dependency or cycle; the extraction analyzer reports the module ready. |
| Transactions | Every cross-module ACID dependency is removed, retained deliberately, or redesigned around durable eventual consistency. |
| Reads | Cross-module joins are replaced by an owner API, consumer-owned projection, or explicitly retained reporting adapter. |
| Delivery | Cross-process side effects use transactional outbox/inbox and idempotent consumers. |
| Data lifecycle | Export, retention, deletion, recovery, and audit responsibilities remain complete after the split. |
| Contracts | HTTP/event compatibility, versioning, timeout, retry, and failure semantics are explicit. |
| Operations | Independent readiness, telemetry, alerting, backup/restore, migration, rollout, and rollback procedures exist. |
| Motivation | Independent scaling, availability, security, release ownership, or storage requirements justify the added distributed-system cost. |

Passing source-boundary analysis does not prove that transactions, data
migration, or operations are ready. It proves only that the in-process code
boundary is suitable for the next design step.

## Current candidates

Assessment date: 2026-08-14.

| Candidate | Source boundary | Data/runtime position | Current decision |
| --- | --- | --- | --- |
| Billing | Extraction analyzer reports ready; no aggregate, repository, or directory leaks. | Provider webhooks, renewal jobs, entitlements, and user role changes still need an explicit consistency and migration design. | Best primary-backend pilot if independent security, release, or availability needs emerge. Do not split only to obtain a separate `DbContext`. |
| Notifications | Extraction analyzer reports ready at module level. Repository/directory and composition findings are owner-side infrastructure evidence, not foreign writers. | Feed/preferences, web-push outbox, live refresh, and user lifecycle require a clear projection and deletion protocol. | Keep in-process until independent delivery scaling or availability justifies duplicated user projections and eventual consistency. |
| Dietologist | Extraction analyzer reports ready; no aggregate, repository, or directory leaks. | Attention signals intentionally use a consumer-owned batch projection over primary data. Moving it would require replicated meal/body/activity projections. | Boundary is clean, but data fan-in makes physical extraction unattractive without a strong team or scaling reason. |
| MailRelay | Already a separate bounded context, process, database, client package, readiness probe, and telemetry source. | PostgreSQL queue is authoritative; RabbitMQ is optional transport acceleration. | Reference implementation for a successfully separated support service. |
| MailInbox | Already a separate bounded context, process, database, client package, readiness probe, and SMTP-ingestion telemetry source. | Owns inbound SMTP/MIME and persistence. | Keep separated; tune dashboards and alerts from observed ingestion volume and latency. |

## Safe extraction sequence

1. Record the business reason and measurable success criteria in an ADR.
2. Run the extraction analyzer and architecture tests; remove every foreign write.
3. Inventory cross-module transactions, joins, personal-data lifecycle, jobs, and provider callbacks.
4. Introduce durable events and consumer-owned projections while still inside the monolith.
5. Rehearse backfill, dual-read/compare, cutover, rollback, and restore on disposable production-like data.
6. Move the process boundary before deleting the old path; observe correctness and lag.
7. Transfer table ownership to the new database only after consumers no longer depend on the shared transaction or join.

Separate `DbContext` types or PostgreSQL schemas may be useful rehearsal tools,
but they are optional. They should not be introduced without a concrete
extraction risk that they reduce.
