---
id: generated.module.notifications
kind: module
status: current
generated_by: .llm-wiki/tools/Build-LlmWikiModulePages.ps1
sources:
  - .llm-wiki/tools/Build-LlmWikiModulePages.ps1
  - .llm-wiki/generated/repository-catalog.json
  - docs/architecture/module-dependencies.json
---

# Notifications

## Graph

- Origin: module-graph
- Dependencies: Users
- Consumers: Authentication, Dietologist, Fasting, RecipeComments, WeeklyGoals

## Source Areas

- `FoodDiary.Application.Abstractions/Notifications`
- `FoodDiary.Application/Notifications`
- `FoodDiary.Domain/Entities/Notifications`
- `FoodDiary.Infrastructure/Persistence/Configurations/Notifications`
- `FoodDiary.Infrastructure/Persistence/Notifications`
- `FoodDiary.Presentation.Api/Features/Notifications`
- `FoodDiary.Resources/Notifications`
- `FoodDiary.Web.Client/src/app/shared/notifications`
- `tests/FoodDiary.Application.Tests/Notifications`
- `tests/FoodDiary.Resources.Tests/Notifications`

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

## Focused Tests

- `tests/FoodDiary.Application.Tests/Notifications/DeliverTestNotificationCommandHandlerTests.cs`
- `tests/FoodDiary.Application.Tests/Notifications/NotificationReadServiceCoverageTests.cs`
- `tests/FoodDiary.Application.Tests/Notifications/NotificationsFeatureTests.MappingAndCleanup.cs`
- `tests/FoodDiary.Application.Tests/Notifications/NotificationsFeatureTests.Preferences.cs`
- `tests/FoodDiary.Application.Tests/Notifications/NotificationsFeatureTests.Queries.cs`
- `tests/FoodDiary.Application.Tests/Notifications/NotificationsFeatureTests.ReadCommands.cs`
- `tests/FoodDiary.Application.Tests/Notifications/NotificationsFeatureTests.WebPush.cs`
- `tests/FoodDiary.Application.Tests/Notifications/NotificationsFeatureTests.cs`
- `tests/FoodDiary.Application.Tests/Notifications/NotificationsValidatorTests.cs`
- `tests/FoodDiary.Presentation.Api.Tests/NotificationsControllerTests.cs`
- `tests/FoodDiary.Resources.Tests/Notifications/NotificationResourceRendererTests.cs`

## Working Rule

Use this page for discovery only. Read the nearest scoped `AGENTS.md` and
verify behavior in source code, tests, and API contract snapshots before
changing the module.
