# Backend Architecture Audit - 2026-07-08

## Status
The backend architecture improvement pass is complete for this session.

The main objective was to tighten application-layer read boundaries without changing HTTP contracts or database schema. The current state is good enough to stop the cleanup phase and switch back to product work or a separately scoped technical initiative.

## Completed
- Current-user scoped handlers now resolve user access consistently through current-user access services instead of parsing request user ids directly.
- Favorite, notification, hydration, lesson, dashboard body, and related read services now use read-model contracts for projection-oriented reads.
- Temporary composite repository method duplicates were removed after read-model contracts took ownership of projection/counter methods.
- Notification existence checks now use `INotificationLookupRepository`; application code no longer depends on `INotificationReadRepository`.
- Architecture guardrails now protect the migrated boundaries and prevent the moved methods from returning to aggregate read contracts.

## Current Boundary Rules
- Use `*ReadModelRepository` for projection reads, counters, summaries, and UI/API read models.
- Use `*LookupRepository` for narrow existence checks that do not need aggregates.
- Use `*WriteRepository` for tracked aggregate mutation paths.
- Avoid full composite `*Repository` dependencies in application services and handlers unless a test helper or infrastructure composition point truly needs to satisfy several contracts at once.
- Keep executable hosts as composition roots; do not move feature logic into hosts or presentation.

## Residual Debt
The remaining broad repository dependencies are not blocking:
- Some command handlers still need aggregate read/write pairs because they mutate domain aggregates after ownership or existence checks.
- Some tests still use composite in-memory repositories for convenience where the same object is intentionally passed as multiple contracts.
- A few older read services still use aggregate read contracts when the read is not yet represented by a dedicated read model. These should be migrated only when that slice is touched for product work.

## Recommended Next Work
Do not continue architecture cleanup as an open-ended task. Future work should be scoped around one of these concrete goals:
- product feature delivery,
- performance tuning for a named endpoint/workflow,
- observability for jobs/outbox/notifications,
- security hardening,
- a single bounded slice migration with tests and guardrails.

Before starting another backend architecture pass, run a fresh audit and pick no more than one bounded slice.
