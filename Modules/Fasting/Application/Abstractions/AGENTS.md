# Fasting Application Abstractions Guidelines

## Scope

Rules for `Modules/Fasting/Application/Abstractions/`.

## Role

- Own internal repository ports, persistence projections, and application read abstractions for Fasting.
- Preserve the legacy `FoodDiary.Application.Abstractions.Fasting` namespace during extraction while all consumers migrate by project reference.

## Boundaries

- Depend only on Fasting Domain and shared result primitives.
- Do not reference Infrastructure, EF Core, hosts, or presentation projects.
- Keep stable cross-module DTOs and services in `Modules/Fasting/Contracts` instead.
