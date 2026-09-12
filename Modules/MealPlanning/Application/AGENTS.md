# Meal Planning Application Module Guidelines

## Scope

Rules for `Modules/MealPlanning/Application/`.

## Role

- Own meal-plan and shopping-list use cases in one cohesive planning module.
- Preserve MealPlans and ShoppingLists as separate logical aggregate owners.
- Keep meal-plan-to-shopping-list generation behind `IShoppingListCreationService`.

## Boundaries

- Do not reference the core `FoodDiary.Application` project.
- Register handlers, validators, and services through `AddMealPlanningApplication` (invoked by the complete Infrastructure module facade).
- Depend on other business areas through their owner contracts, with direct references to used Products/Recipes contracts rather than central umbrella exports; do not reference foreign application implementations.
- Use Meals Domain.Contracts for scalar meal types; do not reference Meals Domain. Preserve existing read capabilities and mapping behavior.
- Keep persistence implementations, HTTP transport, and host configuration outside this project.

Reference Products Domain.Contracts for MeasurementUnit in existing shopping quantities. Preserve unit parsing and generation behavior.
