# Backend Definition Of Done

Date: 2026-03-28
Scope: backend changes in `FoodDiary.Domain`, `FoodDiary.Application`, `FoodDiary.Infrastructure`, `FoodDiary.Presentation.Api`, `FoodDiary.Web.Api`, related backend tests

## Purpose

Use this checklist for every backend change.

The goal is simple:

- keep backend quality repeatable
- reduce review ambiguity
- make critical expectations explicit before merge

This document is the execution artifact for `B04` in `BACKEND_10_OF_10_PLAN.md`.

## Core Rule

A backend task is not done when the code compiles.

A backend task is done when behavior, safety, tests, configuration impact, and operational impact have all been checked at the level appropriate for the change.

## Required Checklist

Mark each item deliberately for every backend PR or local task.

- [ ] The change is implemented in the correct layer and respects dependency direction.
- [ ] New or changed behavior has tests at the right level.
- [ ] Existing relevant tests still pass.
- [ ] If the change affects a critical flow, the matching PostgreSQL-backed integration test was added or updated.
- [ ] If the change touches API payloads, status codes, routes, or error contracts, the presentation/API contract impact was reviewed.
- [ ] If the change touches persistence or queries, relational behavior and PostgreSQL assumptions were reviewed.
- [ ] If the change touches time-based behavior, it follows `BACKEND_TIME_POLICY.md`.
- [ ] If the change touches secrets or options, tracked config remains sanitized and local/deploy setup is still clear.
- [ ] If the change touches migrations, both migration files are included and migration safety was reviewed.
- [ ] If the change affects logs, metrics, tracing, background work, or operational diagnosis, telemetry/runbook impact was reviewed.
- [ ] The change does not silently weaken validation, authorization, or security boundaries.
- [ ] Any deferred gap is written down explicitly, with reason and next step.

## Minimum Expectations By Change Type

### Domain Changes

- Preserve invariants inside aggregates and value objects.
- Add or update domain-level tests for changed rules.
- Do not move business rules into persistence or controllers.

### Application Changes

- Use focused commands and queries.
- Propagate `CancellationToken`.
- Use `IDateTimeProvider` for current UTC time.
- Add or update handler/service tests.

### Infrastructure Changes

- Keep dependency direction inward.
- Review PostgreSQL behavior, relational constraints, and query semantics.
- Add or update infrastructure or PostgreSQL-backed integration tests when behavior changes.

### Presentation/API Changes

- Keep transport concerns in `FoodDiary.Presentation.Api`.
- Preserve error contract consistency.
- Review auth, rate limiting, and user-boundary behavior when relevant.
- Add or update controller/presentation tests and critical API integration coverage when relevant.

### Host Changes

- Keep host concerns in `FoodDiary.Web.Api`.
- Validate new options on startup when practical.
- Review middleware, telemetry, auth wiring, caching, and rate-limiting impact.

## Critical Flow Rule

If a change affects a flow listed in `BACKEND_CRITICAL_FLOW_MATRIX.md`, it is not done until one of these is true:

1. the existing PostgreSQL-backed test was updated and still passes
2. a new PostgreSQL-backed test was added
3. the gap is explicitly documented as deferred with a concrete reason

## Acceptable Deferrals

A deferral is acceptable only when all of the following are true:

- the limitation is real and specific
- the risk is understood
- the missing coverage or safeguard is named explicitly
- the next step is written down in repo docs or the active plan

`Will do later` is not a valid deferral.

## Suggested PR Closeout Format

Use a short summary like this for backend work:

```text
Backend DoD
- Tests updated: yes/no
- Critical flow impact: yes/no
- API contract impact: yes/no
- Config/secret impact: yes/no
- Migration impact: yes/no
- Telemetry/runbook impact: yes/no
- Deferred gaps: none / listed
```

## Notes

- This checklist is intentionally strict for safety-critical backend work.
- Small refactors can apply it proportionally, but should still check the relevant items.
- When in doubt, bias toward explicit tests and explicit documentation.
