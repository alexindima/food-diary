# Statistics Logical Module Guidelines

## Scope

Rules for `Modules/Statistics/`.

## Boundary

- Own Statistics query orchestration, calculation projections, response models, and date normalization.
- Keep the real application assembly under `Application/`; do not create root wrappers or empty symmetric layers.
- Consume Dashboard through a direct `Modules/Dashboard/Contracts` reference; consume Body Metrics and user access through their existing read contracts. Do not use central Application.Abstractions as a Dashboard dependency umbrella.
- Preserve the legacy `FoodDiary.Application.Statistics` assembly name and CLR namespaces.
- Do not add Contracts, Application Abstractions, Domain, Infrastructure, or persistence-model projects unless a separately proven responsibility appears.
- Keep HTTP transport central in `FoodDiary.Presentation.Api` and the optimized Dashboard projection in Modules/Dashboard/Infrastructure.

## Tests

- Keep Statistics-owned query, calculation, validation, and date-semantics tests under this module's `tests/` folder.
- Keep Dashboard composition, HTTP, host, architecture, and shared persistence projection tests in their central test projects.
