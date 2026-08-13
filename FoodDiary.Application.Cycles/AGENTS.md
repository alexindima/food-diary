# Cycles Application Module Guidelines

## Scope

Rules for `FoodDiary.Application.Cycles/`.

## Boundaries

- Own cycle profile, factors, symptoms, bleeding entries, fertility signals, and their use cases.
- Do not reference the core `FoodDiary.Application` project.
- Register handlers, validators, and read services through `AddCyclesModule`.
- Depend on other business areas only through `FoodDiary.Application.Abstractions` contracts.
