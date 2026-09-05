# Notifications Module Guidelines

## Scope

Rules for `Modules/Notifications/`.

## Ownership

- Own notification feed, localized notification text, web-push subscriptions, delivery orchestration and notification cleanup.
- Own notification aggregates/IDs, application ports and payload contracts, persistence models/repositories and web-push provider adapters in their corresponding module layers.
- Use shared Outbox.Abstractions for the lifecycle contract; central Infrastructure retains only the multi-stream engine/claiming/replay responsibilities.
- Preserve notification channels, payloads, text selection, delivery behavior and retry semantics during structural changes.
- User profile preference fields remain owned by Users and are accessed through the existing profile contracts.
- Notification HTTP transport lives in `Modules/Notifications/Presentation`; shared SignalR primitives remain in `FoodDiary.Presentation.Api`, while scheduling and consumers remain in `FoodDiary.JobManager`.
- The shared `FoodDiaryDbContext`, migrations and model snapshot remain central.
