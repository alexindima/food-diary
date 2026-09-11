# BodyMetrics Persistence Model Guidelines

## Scope

Rules for `Modules/BodyMetrics/Infrastructure/Model/`.

## Boundaries

- Own `WeightEntry` and `WaistEntry` EF configurations and explicit model-builder registration.
- Preserve table, column, index, unidirectional User relationship, FK/cascade, conversion, and CLR identity exactly; do not restore inverse User measurement navigations.
- Weight/waist goal mappings belong to Users Infrastructure/Model with the Users-owned goal lifecycle.

- Keep UserId conversions local through Users.Domain.Contracts. User Cascade relationships are composed by BodyMetricsCrossModuleRelationships in central Infrastructure; this model must not reference foreign Domain assemblies.
