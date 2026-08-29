# Fasting Logical Module Guidelines

## Scope

Rules for `Modules/Fasting/`.

## Role

- Own fasting plans, occurrences, check-ins, insights, telemetry, and notification scheduling use cases.
- Keep aggregates and value objects under `Domain/`, use cases under `Application/`, stable cross-module surfaces under `Contracts/`, and persistence implementations under `Infrastructure/`.
- Depend on other business areas only through approved contracts.

## Boundaries

- Do not reference the core `FoodDiary.Application` project.
- Register application behavior through `AddFastingApplication`; executable composition roots use Infrastructure's `AddFastingModule` facade.
- Keep HTTP transport, Hangfire orchestration, host configuration, the shared `FoodDiaryDbContext`, and the central EF migration history outside this module.
- Align implementation namespaces with `FoodDiary.Modules.Fasting.Application.*` and paths under `Application/`.
- Do not place repository ports or domain aggregates in `Contracts/`.
- Consumers outside composition roots must reference `FoodDiary.Modules.Fasting.Contracts`, not implementation services.
- Repository ports and internal persistence read models belong in `Application/Abstractions`; repository implementations belong in `Infrastructure/Persistence`; EF mappings belong in `Infrastructure/Model`.
- Preserve existing Fasting domain CLR namespaces until a separately planned EF migration changes snapshot identity safely.
- The application project's legacy assembly name `FoodDiary.Application.Fasting` is a temporary binary-compatibility detail; use `Fasting` as the module identity and `FoodDiary.Modules.Fasting.*` for new implementation namespaces.
