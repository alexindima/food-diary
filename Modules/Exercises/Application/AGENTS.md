# Exercises Application Module Guidelines

## Scope

Rules for `Modules/Exercises/Application/`.

## Boundaries

- Own exercise entries and their use cases and read models.
- Do not reference the core `FoodDiary.Application` project.
- Register handlers, validators, and read services through `AddExercisesApplication`.
- Depend on other business areas only through `FoodDiary.Application.Abstractions` contracts.
