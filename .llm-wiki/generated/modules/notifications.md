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
- Extracted project: `Modules/Notifications/Application/FoodDiary.Application.Notifications.csproj`
- Business-module dependencies: none observed
- Abstraction-contract dependencies: Users
- Business-module consumers: none observed
- Host/adapter consumers: FoodDiary.Initializer, FoodDiary.JobManager, FoodDiary.Presentation.Api, FoodDiary.Web.Api
- Evidence model: compile-time namespaces plus project/composition source evidence; runtime DI/reflection may be incomplete.

## Source Areas

- `FoodDiary.Presentation.Api/Features/Notifications`
- `Modules/Notifications/Application`
- `Modules/Notifications/Application/Abstractions`
- `Modules/Notifications/Domain`
- `Modules/Notifications/Infrastructure`
- `Modules/Notifications/Infrastructure/Model`

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
- Physical isolation: module-root
- Architecture guardrails: project-reference-matrix
- Declared owned entities: Notification, WebPushSubscription, NotificationWebPushOutboxMessage
- Public contract files: 0
- Observed external consumer groups: 4
- Foreign repositories acquired: guarded where enforcement is explicit; otherwise not inferred from this page

## Public Surface

- Public contract types: 0
- Interfaces: 0
- DTO/read-model/projection types: 0
- Enums: 0
- Exported repository-shaped contracts: 0
- Contracts referencing domain entities: 0
- No public declaration was found in the mapped abstraction areas.

## Focused Tests

Test paths below are discovery evidence, not proof that a boundary assertion executed or passed.

- [behavioral-or-text-match] `Modules/Notifications/tests/FoodDiary.Modules.Notifications.Application.Tests/Notifications/DeliverTestNotificationCommandHandlerTests.cs`
- [behavioral-or-text-match] `Modules/Notifications/tests/FoodDiary.Modules.Notifications.Application.Tests/Notifications/NotificationFactoryTests.cs`
- [behavioral-or-text-match] `Modules/Notifications/tests/FoodDiary.Modules.Notifications.Application.Tests/Notifications/NotificationReadServiceCoverageTests.cs`
- [behavioral-or-text-match] `Modules/Notifications/tests/FoodDiary.Modules.Notifications.Application.Tests/Notifications/NotificationsFeatureTests.MappingAndCleanup.cs`
- [behavioral-or-text-match] `Modules/Notifications/tests/FoodDiary.Modules.Notifications.Application.Tests/Notifications/NotificationsFeatureTests.Preferences.cs`
- [behavioral-or-text-match] `Modules/Notifications/tests/FoodDiary.Modules.Notifications.Application.Tests/Notifications/NotificationsFeatureTests.Queries.cs`
- [behavioral-or-text-match] `Modules/Notifications/tests/FoodDiary.Modules.Notifications.Application.Tests/Notifications/NotificationsFeatureTests.ReadCommands.cs`
- [behavioral-or-text-match] `Modules/Notifications/tests/FoodDiary.Modules.Notifications.Application.Tests/Notifications/NotificationsFeatureTests.WebPush.cs`
- [behavioral-or-text-match] `Modules/Notifications/tests/FoodDiary.Modules.Notifications.Application.Tests/Notifications/NotificationsFeatureTests.cs`
- [behavioral-or-text-match] `Modules/Notifications/tests/FoodDiary.Modules.Notifications.Application.Tests/Notifications/NotificationsValidatorTests.cs`
- [behavioral-or-text-match] `Modules/Notifications/tests/FoodDiary.Modules.Notifications.Domain.Tests/Domain/NotificationInvariantTests.cs`
- [behavioral-or-text-match] `Modules/Notifications/tests/FoodDiary.Modules.Notifications.Infrastructure.Tests/Persistence/NotificationRepositoryTests.cs`
- [behavioral-or-text-match] `Modules/Notifications/tests/FoodDiary.Modules.Notifications.Infrastructure.Tests/Persistence/NotificationWebPushOutboxTests.cs`
- [behavioral-or-text-match] `Modules/Notifications/tests/FoodDiary.Modules.Notifications.Infrastructure.Tests/Services/WebPushClientAdapterTests.cs`
- [behavioral-or-text-match] `Modules/Notifications/tests/FoodDiary.Modules.Notifications.Infrastructure.Tests/Services/WebPushEndpointSecurityTests.cs`
- [behavioral-or-text-match] `Modules/Notifications/tests/FoodDiary.Modules.Notifications.Infrastructure.Tests/Services/WebPushNotificationSenderTests.cs`
- [architecture-boundary] `tests/FoodDiary.ArchitectureTests/NotificationsModuleExtractionTests.cs`
- [presentation] `tests/FoodDiary.Presentation.Api.Tests/NotificationsControllerTests.cs`
- [behavioral-or-text-match] `tests/FoodDiary.Resources.Tests/Notifications/NotificationResourceRendererTests.cs`

## Working Rule

Use this page for discovery only. Read the nearest scoped `AGENTS.md` and
verify behavior in source code, tests, and API contract snapshots before
changing the module.
