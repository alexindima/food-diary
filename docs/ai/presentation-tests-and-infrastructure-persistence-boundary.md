# Presentation test ownership and shared persistence-model boundary

## Scope

This change combines two structural cleanups that share the same solution, lockfile, architecture-test, and verification cost:

- relocate single-module controller and HTTP-mapping tests from the central Presentation test assembly to module-owned test projects;
- review `FoodDiary.Infrastructure/Persistence` and extract only dependency-light shared persistence records and their EF configurations.

The change is structural. Routes, payloads, authorization, mappings, database schema, migrations, outbox processing behavior, and audit/email delivery behavior are unchanged.

## Presentation test ownership

Seventy-three module-owned test source files were moved without duplicating test cases. Thirty-one `FoodDiary.Modules.<Module>.Presentation.Tests` projects now live below the owning module's `tests` folder and reference only that module's Presentation assembly. Marketing has no unambiguous single-owner Presentation test file, so no empty test project was created.

The central `FoodDiary.Presentation.Api.Tests` project retains the shared HTTP kernel and cross-module surface: base controllers, filters, binders, conventions, error/result mapping, telemetry, composite endpoints, shared test senders, and security/route checks. This is an intentional ownership boundary rather than a goal to make the central test project empty.

Architecture tests derive the expected module Presentation test set from the production Presentation projects and verify that each test project references exactly its owner. This prevents new module-only controller tests from drifting back into an assembly with broad cross-module references.

## Infrastructure persistence review

The review classified the remaining central persistence code by responsibility.

Extracted into narrow Shared PersistenceModel projects:

- `FoodDiary.Audit.PersistenceModel`: `AuditEntry`, its EF configuration, and explicit model registration;
- `FoodDiary.Email.PersistenceModel`: `EmailOutboxMessage`, its EF configuration, and explicit model registration;
- `FoodDiary.Outbox.PersistenceModel`: `OutboxReplayAudit`, its EF configuration, and explicit model registration.

These types are shared records rather than feature-owned runtime engines. Their existing CLR namespaces and database mappings are preserved. `FoodDiaryDbContext` applies all three model assemblies explicitly.

Retained centrally after source-usage review:

- `FoodDiaryDbContext`, its partial DbSet declarations, design-time factory, migrations, and snapshot;
- `EfUnitOfWork`, domain-event dispatch interception, and PostgreSQL locking primitives;
- generic outbox claiming, processing policy, replay coordination, and multi-stream orchestration;
- generic audit interception/storage service and email outbox processing/replay adapters;
- the shared Recipe composition transaction lock.

The retained code coordinates multiple modules or implements the shared database runtime. Moving it into any one module or into a model-only assembly would create false ownership and wider dependencies. Feature-specific outbox records, mappings, and stream adapters remain in Images, Notifications, and Gamification.

## Compatibility and verification contract

- No migration or model snapshot is changed; `has-pending-model-changes` must remain clean.
- The complete Presentation test population must remain 825 executed tests across the central project and the 31 module projects.
- Architecture tests must validate the solution inventory, project-reference matrix, module-local Presentation test ownership, root guide coverage, and Docker build contexts.
- Infrastructure unit and PostgreSQL integration suites must validate the extracted EF records and unchanged runtime engines.
- Web API integration tests must validate route, payload, Swagger, and executable-host compatibility.
- Coverage collectors are deliberately not part of this structural verification.

## Wiki observations

The governed Wiki task was initialized after implementation had started, so its initial changed-path baseline includes pre-existing changes from this same work package. That timing limitation is recorded rather than treated as pre-edit evidence. Wiki-generated catalogs remain derived navigation only; the solution, project references, source, tests, and this ownership analysis are authoritative.
