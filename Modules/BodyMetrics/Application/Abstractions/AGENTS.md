# BodyMetrics Application Abstractions Guidelines

## Scope

Rules for `Modules/BodyMetrics/Application/Abstractions/`.

## Boundaries

- Own BodyMetrics repository ports, read capabilities, errors, and projection models.
- Keep public namespaces under `FoodDiary.Application.Abstractions.WeightEntries` and `.WaistEntries` for compatibility.
- Do not expose provider, EF, HTTP, or host concerns.
