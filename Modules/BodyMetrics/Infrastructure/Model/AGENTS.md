# BodyMetrics Persistence Model Guidelines

## Scope

Rules for `Modules/BodyMetrics/Infrastructure/Model/`.

## Boundaries

- Own `WeightEntry` and `WaistEntry` EF configurations and explicit model-builder registration.
- Preserve table, column, index, relationship, conversion, and CLR identity exactly.
- Weight/waist goal mappings stay central while those lifecycle types remain owned by the central `User` aggregate.
