# BodyMetrics Persistence Model Guidelines

## Scope

Rules for `Modules/BodyMetrics/Infrastructure/Model/`.

## Boundaries

- Own `WeightEntry` and `WaistEntry` EF configurations and explicit model-builder registration.
- Preserve table, column, index, unidirectional User relationship, FK/cascade, conversion, and CLR identity exactly; do not restore inverse User measurement navigations.
- Weight/waist goal mappings stay central while those lifecycle types remain owned by the central `User` aggregate.
