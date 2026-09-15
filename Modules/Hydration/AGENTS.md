# Hydration Logical Module Guidelines

## Scope

Rules for `Modules/Hydration/`.

## Boundaries

- Own hydration entries, daily totals, hydration goals, and their use cases.
- Keep the real application assembly at `Application/FoodDiary.Modules.Hydration.Application.csproj`; do not recreate a root module project or an empty wrapper.
- Do not reference the core `FoodDiary.Application` project.
- Register application behavior through `AddHydrationApplication`; composition roots use Infrastructure's `AddHydrationModule` facade.
- Depend on other business areas through their owner Contracts and scalar Domain.Contracts projects.
- Keep migrations and the full migration model in central Infrastructure. Hydration Infrastructure owns the two-entity runtime HydrationDbContext; shared IUnitOfWork coordinates saves and transactions across contexts. Central composition reads remain explicit pilot bridges; owner purge joins the live shared transaction; see ADR 0040.
- Keep `HydrationEntry` and `HydrationEntryId` in `Domain` with canonical project-and-folder namespaces. The module depends on Users Domain.Contracts for scalar `UserId`; do not restore the removed inverse `User.HydrationEntries` navigation.

## Tests

- Keep Hydration-owned application and infrastructure adapter tests under `tests/FoodDiary.Modules.Hydration.*.Tests` in this module.
- Keep focused Hydration domain tests in the module-owned Domain test project.
- Keep shared DbContext, migration, HTTP, host, architecture, and cross-module scenarios in their central test projects.

Users owns the complete User aggregate, all credential/security partials, roles,
role audit and weight/waist goals under `Modules/Users/Domain`. `UserId` lives in
`Modules/Users/Domain.Contracts`, depending only on shared primitives. Consumers
reference the exact owner; shared guards and generic values belong to
`FoodDiary.Domain.Primitives`; module-specific values stay with their owner. Authentication flows/providers, combined UserRepository,
DbContext, migrations and snapshot retain their existing owners. CLR namespaces,
security behavior and EF/HTTP contracts are unchanged. See
`docs/ai/users-domain-extraction.md` for residual seams and verification evidence.

Use canonical FoodDiary.Modules.Hydration project identities and folder namespaces, including tests. Projects are siblings. Preserve historical migration metadata and relational schema.
