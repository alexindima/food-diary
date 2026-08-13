# Hydration Application Module Guidelines

## Scope

Rules for `FoodDiary.Application.Hydration/`.

## Boundaries

- Own hydration entries, daily totals, hydration goals, and their use cases.
- Do not reference the core `FoodDiary.Application` project.
- Register handlers, validators, and read services through `AddHydrationModule`.
- Depend on other business areas only through `FoodDiary.Application.Abstractions` contracts.
