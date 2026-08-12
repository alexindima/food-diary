# Fasting Application Module Guidelines

## Scope

Rules for `FoodDiary.Application.Fasting/`.

## Role

- Own fasting plans, occurrences, check-ins, insights, telemetry, and notification scheduling use cases.
- Depend on other business areas only through `FoodDiary.Application.Abstractions` contracts.

## Boundaries

- Do not reference the core `FoodDiary.Application` project.
- Register handlers, validators, and services through `AddFastingModule`.
- Keep persistence implementations, HTTP transport, Hangfire orchestration, and host configuration outside this project.
