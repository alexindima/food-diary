# Favorites Application Module Guidelines

## Scope

Rules for `Modules/Favorites/Application/`.

## Role

- Own favorite meal, product, and recipe use cases in one cohesive physical module.
- Preserve the three favorite types as separate logical feature areas.
- Depend on other business areas only through `FoodDiary.Application.Abstractions` contracts.

## Boundaries

- Do not reference the core `FoodDiary.Application` project.
- Register handlers, validators, and read services through `AddFavoritesApplication`; the Infrastructure facade exposes `AddFavoritesModule` to composition roots.
- Keep persistence implementations, HTTP transport, and host configuration outside this project.
