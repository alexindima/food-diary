# Favorites Application Module Guidelines

## Scope

Rules for `Modules/Favorites/Application/`.

## Role

- Own favorite meal, product, and recipe use cases in one cohesive physical module.
- Preserve the three favorite types as separate logical feature areas.
- Use module-owned contracts and approved central compatibility contracts for other business areas. Own Favorites repository/source-reader ports in `Abstractions` and public read projections in `../Contracts`; exclude nested Abstractions sources from this project's compilation.
- Reference Meals Domain directly for MealType used by the existing favorite-meal mappings; this does not add aggregate mutation capabilities.

## Boundaries

- Do not reference the core `FoodDiary.Application` project.
- Register handlers, validators, and read services through `AddFavoritesApplication`; the Infrastructure facade exposes `AddFavoritesModule` to composition roots.
- Keep persistence implementations, HTTP transport, and host configuration outside this project.

Reference Products Domain for the existing quality calculation and Products Domain.Contracts for ProductType and MeasurementUnit. This approved scoring dependency does not authorize foreign Product aggregate mutation; preserve all mapping behavior.
