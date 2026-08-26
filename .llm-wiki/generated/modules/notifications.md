---
id: generated.module.notifications
kind: module
status: current
generated_by: .llm-wiki/tools/Build-LlmWikiModulePages.ps1
sources:
  - .llm-wiki/tools/Build-LlmWikiModulePages.ps1
  - .llm-wiki/generated/repository-catalog.json
  - docs/architecture/module-dependencies.json
  - docs/architecture/backend-modules.json
---

# Notifications

## Graph

- Origin: extracted-project
- Extracted project: `FoodDiary.Application.Notifications/FoodDiary.Application.Notifications.csproj`
- Business-module dependencies: none observed
- Abstraction-contract dependencies: none observed
- Business-module consumers: none observed
- Host/adapter consumers: FoodDiary.Initializer, FoodDiary.Integrations, FoodDiary.JobManager, FoodDiary.Presentation.Api, FoodDiary.Web.Api
- Evidence model: compile-time namespaces plus project/composition source evidence; runtime DI/reflection may be incomplete.

## Source Areas

- `FoodDiary.Application.Abstractions/Notifications`
- `FoodDiary.Domain/Entities/Notifications`
- `FoodDiary.Infrastructure/Persistence/Configurations/Notifications`
- `FoodDiary.Infrastructure/Persistence/Notifications`
- `FoodDiary.Presentation.Api/Features/Notifications`

## HTTP Surface

### NotificationPushController

Source: `FoodDiary.Presentation.Api/Features/Notifications/NotificationPushController.cs`

- `GET /api/v{version:apiVersion}/notifications/push/config`
- `GET /api/v{version:apiVersion}/notifications/push/subscriptions`
- `PUT /api/v{version:apiVersion}/notifications/push/subscription`
- `DELETE /api/v{version:apiVersion}/notifications/push/subscription`

### NotificationsController

Source: `FoodDiary.Presentation.Api/Features/Notifications/NotificationsController.cs`

- `GET /api/v{version:apiVersion}/notifications`
- `GET /api/v{version:apiVersion}/notifications/unread-count`
- `PUT /api/v{version:apiVersion}/notifications/{notificationId:guid}/read`
- `PUT /api/v{version:apiVersion}/notifications/read-all`
- `POST /api/v{version:apiVersion}/notifications/test/schedule`
- `GET /api/v{version:apiVersion}/notifications/preferences`
- `PUT /api/v{version:apiVersion}/notifications/preferences`

## Boundary Health

- Role: aggregate-owner
- Physical isolation: project
- Architecture guardrails: project-reference-matrix
- Declared owned entities: Notification, WebPushSubscription, NotificationWebPushOutboxMessage
- Public contract files: 41
- Observed external consumer groups: 5
- Foreign repositories acquired: guarded where enforcement is explicit; otherwise not inferred from this page

## Public Surface

- Public contract types: 41
- Interfaces: 22
- DTO/read-model/projection types: 2
- Enums: 0
- Exported repository-shaped contracts: 9
- Contracts referencing domain entities: 6
- `class NotificationErrors`
- `class NotificationPayloads`
- `class NotificationPayloadSerializer`
- `class NotificationTargetUrlResolver`
- `class NotificationTypes`
- `class WebPushDeliveryLimits`
- `interface INotificationCleanupService`
- `interface INotificationClientRefreshService`
- `interface INotificationDeduplicationService`
- `interface INotificationLookupRepository`
- `interface INotificationPusher`
- `interface INotificationReadModelRepository`
- `interface INotificationReadRepository`
- `interface INotificationRepository`
- `interface INotificationTestScheduler`
- `interface INotificationTextRenderer`
- `interface INotificationWebPushOutbox`
- `interface INotificationWebPushOutboxProcessor`
- `interface INotificationWriter`
- `interface INotificationWriteRepository`
- `interface ITestNotificationDeliveryDispatcher`
- `interface IWebPushConfigurationProvider`
- `interface IWebPushDeliveryAudienceService`
- `interface IWebPushNotificationSender`
- `interface IWebPushSubscriptionReadModelRepository`
- `interface IWebPushSubscriptionReadRepository`
- `interface IWebPushSubscriptionRepository`
- `interface IWebPushSubscriptionWriteRepository`
- `record DietologistInvitationDecisionNotificationPayload`
- `record DietologistInvitationReceivedNotificationPayload`
- ... 11 more type(s)

## Focused Tests

Test paths below are discovery evidence, not proof that a boundary assertion executed or passed.

- [behavioral-or-text-match] `tests/FoodDiary.Application.Tests/Notifications/DeliverTestNotificationCommandHandlerTests.cs`
- [behavioral-or-text-match] `tests/FoodDiary.Application.Tests/Notifications/NotificationFactoryTests.cs`
- [behavioral-or-text-match] `tests/FoodDiary.Application.Tests/Notifications/NotificationReadServiceCoverageTests.cs`
- [behavioral-or-text-match] `tests/FoodDiary.Application.Tests/Notifications/NotificationsFeatureTests.MappingAndCleanup.cs`
- [behavioral-or-text-match] `tests/FoodDiary.Application.Tests/Notifications/NotificationsFeatureTests.Preferences.cs`
- [behavioral-or-text-match] `tests/FoodDiary.Application.Tests/Notifications/NotificationsFeatureTests.Queries.cs`
- [behavioral-or-text-match] `tests/FoodDiary.Application.Tests/Notifications/NotificationsFeatureTests.ReadCommands.cs`
- [behavioral-or-text-match] `tests/FoodDiary.Application.Tests/Notifications/NotificationsFeatureTests.WebPush.cs`
- [behavioral-or-text-match] `tests/FoodDiary.Application.Tests/Notifications/NotificationsFeatureTests.cs`
- [behavioral-or-text-match] `tests/FoodDiary.Application.Tests/Notifications/NotificationsValidatorTests.cs`
- [architecture-boundary] `tests/FoodDiary.ArchitectureTests/NotificationsModuleExtractionTests.cs`
- [presentation] `tests/FoodDiary.Presentation.Api.Tests/NotificationsControllerTests.cs`
- [behavioral-or-text-match] `tests/FoodDiary.Resources.Tests/Notifications/NotificationResourceRendererTests.cs`

## Working Rule

Use this page for discovery only. Read the nearest scoped `AGENTS.md` and
verify behavior in source code, tests, and API contract snapshots before
changing the module.
