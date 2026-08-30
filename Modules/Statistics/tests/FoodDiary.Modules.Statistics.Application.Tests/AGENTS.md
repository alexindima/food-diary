# Statistics Application Test Guidelines

## Scope

Rules for `Modules/Statistics/tests/FoodDiary.Modules.Statistics.Application.Tests/`.

## Boundary

- Test Statistics-owned queries, validation, model mapping, failure propagation, cancellation forwarding, and date semantics.
- Use read-contract substitutes; do not reproduce Dashboard or Body Metrics persistence behavior here.
- Preserve the legacy test assembly identity required by `InternalsVisibleTo`.
