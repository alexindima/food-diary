# Hydration Logical Module Guidelines

## Scope

Rules for `Modules/Hydration/`.

## Boundaries

- Own hydration entries, daily totals, hydration goals, and their use cases.
- Keep the real application assembly at `Application/FoodDiary.Modules.Hydration.Application.csproj`; do not recreate a root module project or an empty wrapper.
- Do not reference the core `FoodDiary.Application` project.
- Register application behavior through `AddHydrationApplication`; composition roots use Infrastructure's `AddHydrationModule` facade.
- Depend on other business areas only through `FoodDiary.Application.Abstractions` contracts.
- Keep the shared `FoodDiaryDbContext`, migrations, and model snapshot in central Infrastructure.
- Keep `HydrationEntry` and `HydrationEntryId` in `Domain` with their legacy CLR namespaces. The module depends one-way on Users-owned `User`/`UserId`; do not restore the removed inverse `User.HydrationEntries` navigation.
- Preserve legacy `FoodDiary.Application.Hydration.*` and Hydration domain CLR namespaces during this extraction.

## Tests

- Keep Hydration-owned application and infrastructure adapter tests under `tests/FoodDiary.Modules.Hydration.*.Tests` in this module.
- Keep focused Hydration domain tests in the module-owned Domain test project.
- Keep shared DbContext, migration, HTTP, host, architecture, and cross-module scenarios in their central test projects.

Users owns the complete User aggregate, all credential/security partials, roles,
role audit and weight/waist goals under `Modules/Users/Domain`. `UserId` lives in
`Modules/Users/Domain.Contracts`, depending only on shared primitives. Consumers
reference the exact owner; central Domain retains shared constants and values without
an aggregate re-export. Authentication flows/providers, combined UserRepository,
DbContext, migrations and snapshot retain their existing owners. CLR namespaces,
security behavior and EF/HTTP contracts are unchanged. See
`docs/ai/users-domain-extraction.md` for residual seams and verification evidence.
