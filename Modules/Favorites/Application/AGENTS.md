# Favorites Application Module Guidelines

## Scope

Rules for `Modules/Favorites/Application/`.

## Role

- Own favorite meal, product, and recipe use cases in one cohesive physical module.
- Preserve the three favorite types as separate logical feature areas.
- Use module-owned contracts and approved central compatibility contracts for other business areas. Own Favorites repository/source-reader ports in `Abstractions` and public read projections in `../Contracts`; exclude nested Abstractions sources from this project's compilation.
- Use Meals Domain.Contracts for scalar meal types; do not reference Meals Domain. Preserve existing read capabilities and mapping behavior.

## Boundaries

- Do not reference the core `FoodDiary.Application` project.
- Register handlers, validators, and read services through `AddFavoritesApplication`; the Infrastructure facade exposes `AddFavoritesModule` to composition roots.
- Keep persistence implementations, HTTP transport, and host configuration outside this project.

Reference Products FoodQuality for the existing quality calculation and Products Domain.Contracts for ProductType and MeasurementUnit. This approved scoring dependency does not authorize foreign Product aggregate mutation; preserve all mapping behavior.
