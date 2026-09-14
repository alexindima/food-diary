# BodyMetrics Persistence Model Guidelines

## Scope

Rules for `Modules/BodyMetrics/PersistenceModel/`.

## Boundaries

- Use `FoodDiary.Modules.BodyMetrics.PersistenceModel` with project-relative namespaces.
- Own `WeightEntry` and `WaistEntry` EF configurations and explicit model-builder registration.
- Preserve table, column, index, unidirectional User relationship, FK/cascade and conversion identities exactly; do not restore inverse User measurement navigations.
- Weight/waist goal mappings belong to Users Infrastructure/Model with the Users-owned goal lifecycle.

- Keep UserId conversions local through Users.Domain.Contracts. User Cascade relationships are composed by BodyMetricsCrossModuleRelationships in central Infrastructure; this model must not reference foreign Domain assemblies.
