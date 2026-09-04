# Lessons Logical Module Guidelines

## Scope

Rules for `Modules/Lessons/`.

## Boundaries

- Own nutrition lessons, user lesson progress, application use cases, administration contracts, persistence ports, adapters, and EF configuration.
- Keep the real application assembly at `Application/FoodDiary.Modules.Lessons.Application.csproj`; do not create a root wrapper project.
- Preserve legacy `FoodDiary.Application.Lessons.*`, `FoodDiary.Application.Abstractions.Lessons.*`, and `FoodDiary.Domain.*` CLR namespaces and the `FoodDiary.Application.Lessons` application assembly name.
- External business modules consume only `Contracts`; repository ports and persistence projections remain internal module abstractions.
- Keep the shared `FoodDiaryDbContext`, historical migrations, and model snapshot in central Infrastructure.
- Composition roots register the complete module through Infrastructure's `AddLessonsModule` facade.

## Tests

- Keep Lessons-owned application, domain, and infrastructure adapter tests under `Modules/Lessons/tests`.
- Keep shared DbContext, migrations, achievement composition, HTTP, host, architecture, and cross-module scenarios in central test projects.

## Error ownership

Feature error factories belong to their existing owner contracts; call them directly.
The corresponding central Errors facades are retired. Preserve exact codes, messages,
kinds and parameter formatting. Reference the owner explicitly; this grants no foreign
repository or aggregate capability. See docs/ai/feature-error-retirement.md.
