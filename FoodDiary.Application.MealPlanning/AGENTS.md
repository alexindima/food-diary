# Meal Planning Application Module Guidelines

## Scope

Rules for `FoodDiary.Application.MealPlanning/`.

## Role

- Own meal-plan and shopping-list use cases in one cohesive planning module.
- Preserve MealPlans and ShoppingLists as separate logical aggregate owners.
- Keep meal-plan-to-shopping-list generation behind `IShoppingListCreationService`.

## Boundaries

- Do not reference the core `FoodDiary.Application` project.
- Register handlers, validators, and services through `AddMealPlanningModule`.
- Depend on other business areas only through `FoodDiary.Application.Abstractions` contracts.
- Keep persistence implementations, HTTP transport, and host configuration outside this project.
