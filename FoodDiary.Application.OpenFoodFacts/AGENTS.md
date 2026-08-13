# OpenFoodFacts Application Module Guidelines

## Scope

Rules for `FoodDiary.Application.OpenFoodFacts/`.

## Boundaries

- Own OpenFoodFacts cached search orchestration and public search use cases.
- Keep external provider and persistence implementations behind Application.Abstractions contracts.
- Do not reference the core `FoodDiary.Application` project.
- Register handlers, validators, and cached search through `AddOpenFoodFactsModule`.
