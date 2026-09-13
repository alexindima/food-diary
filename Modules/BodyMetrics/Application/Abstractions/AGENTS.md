# BodyMetrics Application Abstractions Guidelines

## Scope

Rules for `Modules/BodyMetrics/Application/Abstractions/`.

## Boundaries

- Own BodyMetrics repository ports, errors, and internal persistence projections.
- Public weight/waist read capabilities and entry/summary models belong to BodyMetrics.Contracts.
- Keep public namespaces under `FoodDiary.Application.Abstractions.WeightEntries` and `.WaistEntries` for compatibility.
- Do not expose provider, EF, HTTP, or host concerns.
