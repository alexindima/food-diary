# TDEE Application Module Guidelines

## Scope

Rules for `FoodDiary.Application.Tdee/`.

## Boundaries

- Own TDEE calculation, insight models, and their use cases.
- Depend on Exercises through the `FoodDiary.Application.Exercises` project and on other business areas through abstractions.
- Do not reference the core `FoodDiary.Application` project.
- Register handlers, validators, and profile services through `AddTdeeModule`.
