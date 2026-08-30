# Wearables Application Module Guidelines

## Scope

Rules for `Modules/Wearables/Application/`.

## Role

- Own wearable connection, OAuth, synchronization, and daily-summary use cases.
- Depend on other business areas only through `FoodDiary.Application.Abstractions` contracts.

## Boundaries

- Do not reference the core `FoodDiary.Application` project.
- Register handlers and internal read services through `AddWearablesApplication`; complete module registration belongs to Infrastructure.
- Keep provider clients, persistence implementations, token protection, HTTP transport, and host configuration outside this project.
