# Fasting Logical Module Guidelines

## Scope

Rules for `Modules/Fasting/`.

## Role

- Own fasting plans, occurrences, check-ins, insights, telemetry, and notification scheduling use cases.
- The application assembly is the real project `Application/FoodDiary.Modules.Fasting.Application.csproj`; do not recreate a root module project.
- Keep aggregates and value objects under `Domain/`, use cases under `Application/`, stable cross-module surfaces under `Contracts/`, and persistence implementations under `Infrastructure/`.
- Depend on other business areas only through approved contracts.

## Boundaries

- Do not reference the core `FoodDiary.Application` project.
- Register application behavior through `AddFastingApplication`; executable composition roots use Infrastructure's `AddFastingModule` facade.
- Keep HTTP transport in this module's Presentation project. Hangfire orchestration, host configuration, the shared `FoodDiaryDbContext`, and the central EF migration history remain outside this module.
- Align implementation namespaces with `FoodDiary.Modules.Fasting.Application.*` and paths under `Application/`.
- Do not place repository ports or domain aggregates in `Contracts/`.
- Consumers outside composition roots must reference `FoodDiary.Modules.Fasting.Contracts`, not implementation services.
- Repository ports and internal persistence read models belong in `Application.Abstractions`; repository implementations belong in `Infrastructure/Persistence`; EF mappings belong in `PersistenceModel`.
- Domain namespaces match the project and folders. Keep the current EF snapshot aligned with runtime CLR identity; preserve historical migration metadata and relational schema.

## Tests

- Keep Fasting-owned application, domain, and infrastructure adapter tests under `tests/FoodDiary.Modules.Fasting.*.Tests` in this module.
- Keep shared DbContext, migration, HTTP, host, JobManager, architecture, and cross-module scenarios in their central test projects.

Application consumes scalar Users types through Users.Domain.Contracts and semantic
capabilities through Users.Contracts. Do not reference the aggregate-bearing
Users.Domain assembly for these types.

Use the canonical project name as the namespace root and match folders. Projects are siblings. Public owner use cases are Contracts requests dispatched through ISender; keep outbound source ports and reusable algorithms separate. Preserve authorization, cancellation, wire shapes and persistence semantics.

Notification and telemetry cleanup jobs dispatch owner requests without the transactional-command marker. Notification persistence and post-commit ordering remain in SendFastingNotificationsCommandHandler; cleanup keeps independently committed bulk-delete batches.
