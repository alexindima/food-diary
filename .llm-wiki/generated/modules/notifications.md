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
- Host/adapter consumers: FoodDiary.Initializer, FoodDiary.JobManager, FoodDiary.Web.Api
- Evidence model: compile-time namespaces plus project/composition source evidence; runtime DI/reflection may be incomplete.

## Source Areas

- `Modules/Notifications/Application`
- `Modules/Notifications/Application/Abstractions`
- `Modules/Notifications/Domain`
- `Modules/Notifications/Infrastructure`
- `Modules/Notifications/Infrastructure/Model`
- `Modules/Notifications/Presentation`

## HTTP Surface

No literal attribute-routed controller was associated with this module.
## Boundary Health

- Role: aggregate-owner
- Physical isolation: module-root
- Architecture guardrails: project-reference-matrix
- Declared owned entities: Notification, WebPushSubscription, NotificationWebPushOutboxMessage
- Public contract files: 41
- Observed external consumer groups: 3
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
- [behavioral-or-text-match] `Modules/Notifications/tests/FoodDiary.Modules.Notifications.Domain.Tests/NotificationsIdConversionTests.cs`
- [behavioral-or-text-match] `Modules/Notifications/tests/FoodDiary.Modules.Notifications.Infrastructure.Tests/Persistence/NotificationRepositoryTests.cs`
- [behavioral-or-text-match] `Modules/Notifications/tests/FoodDiary.Modules.Notifications.Infrastructure.Tests/Persistence/NotificationWebPushOutboxTests.cs`
- [behavioral-or-text-match] `Modules/Notifications/tests/FoodDiary.Modules.Notifications.Infrastructure.Tests/Persistence/OutboxReplayStreamTests.cs`
- [behavioral-or-text-match] `Modules/Notifications/tests/FoodDiary.Modules.Notifications.Infrastructure.Tests/Resources/NotificationResourceRendererTests.cs`
- [behavioral-or-text-match] `Modules/Notifications/tests/FoodDiary.Modules.Notifications.Infrastructure.Tests/Resources/ResourceContractTests.cs`
- [behavioral-or-text-match] `Modules/Notifications/tests/FoodDiary.Modules.Notifications.Infrastructure.Tests/Services/WebPushClientAdapterTests.cs`
- [behavioral-or-text-match] `Modules/Notifications/tests/FoodDiary.Modules.Notifications.Infrastructure.Tests/Services/WebPushEndpointSecurityTests.cs`
- [behavioral-or-text-match] `Modules/Notifications/tests/FoodDiary.Modules.Notifications.Infrastructure.Tests/Services/WebPushNotificationSenderTests.cs`
- [presentation] `Modules/Notifications/tests/FoodDiary.Modules.Notifications.Presentation.Tests/NotificationHttpMappingsTests.cs`
- [presentation] `Modules/Notifications/tests/FoodDiary.Modules.Notifications.Presentation.Tests/NotificationPushControllerTests.cs`
- [presentation] `Modules/Notifications/tests/FoodDiary.Modules.Notifications.Presentation.Tests/NotificationPusherTests.cs`
- [presentation] `Modules/Notifications/tests/FoodDiary.Modules.Notifications.Presentation.Tests/NotificationsControllerTests.cs`
- [presentation] `Modules/Notifications/tests/FoodDiary.Modules.Notifications.Presentation.Tests/NotificationsPresentationCompositionTests.cs`
- [architecture-boundary] `tests/FoodDiary.ArchitectureTests/NotificationsModuleExtractionTests.cs`

## Working Rule

Use this page for discovery only. Read the nearest scoped `AGENTS.md` and
verify behavior in source code, tests, and API contract snapshots before
changing the module.
