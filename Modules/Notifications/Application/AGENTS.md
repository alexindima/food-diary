# Notifications Application Module Guidelines

## Scope

Rules for `Modules/Notifications/Application/`.

## Role

- Own notification feed, preferences, web-push subscription and delivery orchestration use cases.
- Depend on other business areas only through `FoodDiary.Application.Abstractions` contracts.
- Keep persistence, provider implementations, HTTP transport and host configuration outside this project.

## Boundaries

- Do not reference the core `FoodDiary.Application` project.
- Register module handlers, validators and application services through `AddNotificationsModule`.
- Keep notification aggregate mutation inside this module; consumers use semantic notification contracts.

MarkNotificationRead intentionally preserves its existing Dietologist.InvitationNotFound
error for missing/foreign notifications. Call DietologistErrors through the explicit
owner reference; this compatibility cleanup does not change the public error or grant
Dietologist repository access. See docs/ai/feature-error-retirement.md.
