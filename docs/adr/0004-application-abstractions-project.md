# ADR 0004: Application Abstractions Project

## Status
Accepted

## Context
The primary application layer needs ports and shared application-facing models that infrastructure, integrations, resources, and other adapters can implement or consume.

Putting all ports directly in `FoodDiary.Application` would force more projects to reference concrete application implementation. Putting them in infrastructure would invert dependency direction.

## Decision
Keep `FoodDiary.Application.Abstractions` as the primary port/model boundary for the main FoodDiary backend.

Feature-specific abstractions should live near their feature. Only truly cross-feature contracts belong under `Common`.

## Consequences
Benefits:
- Infrastructure and integrations can implement ports without depending on application implementation.
- Application implementation stays separated from adapter contracts.
- Architecture tests can prevent shared buckets from regrowing unintentionally.

Tradeoffs:
- Developers must choose between feature-local abstractions and `Common`.
- Some types move between projects when a contract becomes shared or feature-specific.
