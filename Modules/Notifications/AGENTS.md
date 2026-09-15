# Notifications Module Guidelines

## Scope

Rules for `Modules/Notifications/`.

## Ownership

- Own notification feed, localized notification text, web-push subscriptions, delivery orchestration and notification cleanup.
- Own notification aggregates/IDs, application ports and payload contracts, persistence models/repositories and web-push provider adapters in their corresponding module layers.
- Use shared Outbox.Abstractions for the lifecycle contract; shared Outbox.Infrastructure owns generic processing/claiming; central Infrastructure retains multi-stream replay coordination.
- Preserve notification channels, payloads, text selection, delivery behavior and retry semantics during structural changes.
- User profile preference fields remain owned by Users and are accessed through the existing profile contracts.
- Notification HTTP and SignalR transport lives in `Modules/Notifications/Presentation`; only reusable SignalR identity plumbing remains in `FoodDiary.Presentation.Api`, while scheduling and consumers remain in `FoodDiary.JobManager`.
- The shared `FoodDiaryDbContext`, migrations and model snapshot remain central.
- Foreign business modules consume Notifications.Contracts; repository and delivery ports remain in Application.Abstractions.

Shared outbox claiming, processing, policy, options and telemetry now belong to `Shared/FoodDiary.Outbox.Infrastructure` (see its AGENTS.md). Images, Notifications and Gamification Infrastructure reference that narrow runtime, never central Infrastructure, including transitively. Central Infrastructure retains replay coordination and the email adapter. The runtime checks `IModuleScopeGuard` on coordinated contexts; owner callbacks and dedicated-context clean-entry checks remain in force.

Feed and preferences operations live in handlers; preference mapping is pure and shared. CleanupExpiredNotificationsCommand commits one batch only when rows were deleted. JobManager owns the repeated batch loop and dispatches through ISender.
