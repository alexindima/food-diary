# Statistics Application Test Guidelines

## Scope

Rules for `Modules/Statistics/tests/FoodDiary.Modules.Statistics.Application.Tests/`.

## Boundary

- Test Statistics-owned queries, validation, model mapping, failure propagation, cancellation forwarding, and date semantics.
- Use read-contract substitutes; do not reproduce Dashboard or Body Metrics persistence behavior here.
- Use canonical project identities and folder namespaces.

Current module convention: all projects use `FoodDiary.Modules.Statistics.<Project>` assembly identities and namespaces matching their folders, including tests. Preserve historical migration metadata and database/HTTP contracts during namespace moves.
