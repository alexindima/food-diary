# Recipe Community Application Module Guidelines

## Scope

Rules for `Modules/RecipeCommunity/Application/`.

## Role

- Own recipe comment and recipe like use cases in one cohesive social-interaction module.
- Preserve comments and likes as separate logical feature areas.
- Depend on Recipes and Users only through `FoodDiary.Application.Abstractions` contracts.

## Boundaries

- Do not reference the core `FoodDiary.Application` project.
- Register handlers, validators, and read services through `AddRecipeCommunityApplication`; hosts use Infrastructure `AddRecipeCommunityModule`.
- Keep persistence implementations, HTTP transport, and host configuration outside this project.
