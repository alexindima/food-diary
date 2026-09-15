# Recipe Community Application Module Guidelines

## Scope

Rules for `Modules/RecipeCommunity/Application/`.

## Role

- Own recipe comment and recipe like use cases in one cohesive social-interaction module.
- Preserve comments and likes as separate logical feature areas.
- Depend on Recipes and Users through their semantic contracts; reference Recipes.Contracts directly instead of relying on central Application.Abstractions to re-export it.

## Boundaries

- Do not reference the core `FoodDiary.Application` project.
- Register handlers, validators, and read services through `AddRecipeCommunityApplication`; hosts use Infrastructure `AddRecipeCommunityModule`.
- Keep persistence implementations, HTTP transport, and host configuration outside this project.

Current module convention: all projects use `FoodDiary.Modules.RecipeCommunity.<Project>` assembly identities and namespaces matching their folders, including tests. Preserve historical migration metadata and database/HTTP contracts during namespace moves.

Comment and like reads live in their handlers. GetRecipeLikeStatus checks recipe access before reading any like data and returns the existing Recipe.NotFound error for missing or inaccessible recipes.
