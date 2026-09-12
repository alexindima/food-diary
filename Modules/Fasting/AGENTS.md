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
- Repository ports and internal persistence read models belong in `Application/Abstractions`; repository implementations belong in `Infrastructure/Persistence`; EF mappings belong in `Infrastructure/Model`.
- Preserve existing Fasting domain CLR namespaces until a separately planned EF migration changes snapshot identity safely.
- The application project's legacy assembly name `FoodDiary.Application.Fasting` is a temporary binary-compatibility detail; its semantic MSBuild project name is `FoodDiary.Modules.Fasting.Application`. Use `Fasting` as the module identity and `FoodDiary.Modules.Fasting.*` for new implementation namespaces.

## Tests

- Keep Fasting-owned application, domain, and infrastructure adapter tests under `tests/FoodDiary.Modules.Fasting.*.Tests` in this module.
- Keep shared DbContext, migration, HTTP, host, JobManager, architecture, and cross-module scenarios in their central test projects.

Application consumes scalar Users types through Users.Domain.Contracts and semantic
capabilities through Users.Contracts. Do not reference the aggregate-bearing
Users.Domain assembly for these types.
