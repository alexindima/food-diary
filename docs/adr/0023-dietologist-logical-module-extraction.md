# ADR 0023: Extract Dietologist into a vertical logical module

- Status: Accepted
- Date: 2026-08-30

## Context

Dietologist owned invitations, permissions, recommendations, client tasks, attention projections, and their persistence, but ownership was spread across central projects. The relationship exposes health data and requires an auditable authorization boundary.

## Decision

Move Dietologist-owned Application, application ports/projections, Domain, EF persistence model, repositories, and registration facade under `Modules/Dietologist`. Preserve the legacy `FoodDiary.Application.Dietologist` assembly and existing CLR namespaces. Hosts register `AddDietologistModule()` from module Infrastructure; central Infrastructure keeps the shared `FoodDiaryDbContext`, migrations, snapshot, audit adapter, and migration host.

No Contracts project is created because no stable cross-module API distinct from adapter-facing application ports was proven. Central shared error aggregation remains in `FoodDiary.Application.Abstractions` because partial `Errors` types cannot span assemblies.

Health projections continue to require relationship validation and explicit `DietologistPermissions`; the attention projection remains a consumer-owned batch read over shared primary data.

## Consequences

- Project-reference guardrails enforce the vertical boundary.
- EF identity, table/schema names, and historical migrations remain unchanged; no migration is created.
- HTTP/host/shared-DbContext tests remain central; module-owned tests move under the module.
- Separate deployment still requires replicated health/activity projections and an explicit consistency design.
