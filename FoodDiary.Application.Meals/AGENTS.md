# Meals Application Module Guidelines

## Scope

Rules for `FoodDiary.Application.Meals/`.

## Ownership

- Own meal diary use cases for `Meal`, `MealItem`, `MealAiSession`, and `MealAiItem`.
- Keep the application and HTTP vocabulary consistently `Meal`/`Meals`.
- Expose narrow read capabilities for dashboards, exports, calculations, favorites, and external catalog adapters.
- Do not expose aggregate repositories to foreign application modules.

## Boundaries

- Depend only on Application Abstractions, Domain, Mediator, and explicitly approved application modules.
- Keep provider, persistence, and HTTP transport concerns outside this project.
- Register module handlers, validators, and services through `AddMealsModule()`.
