# Statistics Logical Module Guidelines

## Scope

Rules for `Modules/Statistics/`.

## Boundary

- Own Statistics query orchestration, calculation projections, response models, and date normalization.
- Keep the real application assembly under `Application/`; do not create root wrappers or empty symmetric layers.
- Consume nutrition statistics through a direct `Modules/Meals/Contracts` reference; consume Body Metrics and user access through their existing read contracts. Do not use central Application.Abstractions as a nutrition dependency umbrella.
- Use canonical `FoodDiary.Modules.Statistics.<Project>` assembly identities and folder namespaces.
- Do not add Contracts, Application Abstractions, Domain, Infrastructure, or persistence-model projects unless a separately proven responsibility appears.
- Keep HTTP transport in `Modules/Statistics/Presentation` and nutrition aggregation in its Meals owner.

## Tests

- Keep Statistics-owned query, calculation, validation, and date-semantics tests under this module's `tests/` folder.
- Keep Dashboard composition, HTTP, host, architecture, and shared persistence projection tests in their central test projects.

Current module convention: all projects use `FoodDiary.Modules.Statistics.<Project>` assembly identities and namespaces matching their folders, including tests. Preserve historical migration metadata and database/HTTP contracts during namespace moves.
