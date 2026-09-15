# BodyMetrics Application Abstractions Guidelines

## Scope

Rules for `Modules/BodyMetrics/Application.Abstractions/`.

## Boundaries

- Own BodyMetrics repository ports, errors, and internal persistence projections.
- Public weight/waist read capabilities and entry/summary models belong to BodyMetrics.Contracts.
- Use `FoodDiary.Modules.BodyMetrics.Application.Abstractions` with feature-relative namespaces. Keep separate write and read-model ports; public read requests and immutable results belong to Contracts; do not export service interfaces.
- Do not expose provider, EF, HTTP, or host concerns.
