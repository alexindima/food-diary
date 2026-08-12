# Notifications Application Module Guidelines

## Scope

Rules for `FoodDiary.Application.Notifications/`.

## Role

- Own notification feed, preferences, web-push subscription and delivery orchestration use cases.
- Depend on other business areas only through `FoodDiary.Application.Abstractions` contracts.
- Keep persistence, provider implementations, HTTP transport and host configuration outside this project.

## Boundaries

- Do not reference the core `FoodDiary.Application` project.
- Register module handlers, validators and application services through `AddNotificationsModule`.
- Keep notification aggregate mutation inside this module; consumers use semantic notification contracts.
