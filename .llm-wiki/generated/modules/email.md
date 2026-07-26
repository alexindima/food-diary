---
id: generated.module.email
kind: module
status: current
generated_by: .llm-wiki/tools/Build-LlmWikiModulePages.ps1
sources:
  - .llm-wiki/tools/Build-LlmWikiModulePages.ps1
  - .llm-wiki/generated/repository-catalog.json
  - docs/architecture/module-dependencies.json
---

# Email

## Graph

- Origin: module-graph
- Dependencies: none
- Consumers: Admin, Authentication

## Source Areas

- `FoodDiary.Application.Abstractions/Email`
- `FoodDiary.Application/Email`
- `FoodDiary.Infrastructure/Persistence/Configurations/Email`
- `FoodDiary.Infrastructure/Persistence/Email`
- `MailRelay/FoodDiary.MailRelay.Presentation/Features/Email`

## HTTP Surface

### MailRelayDeliveryEventsController

Source: `MailRelay/FoodDiary.MailRelay.Presentation/Features/Email/MailRelayDeliveryEventsController.cs`

- `GET /api/email/events`
- `POST /api/email/events`

### MailRelayMessagesController

Source: `MailRelay/FoodDiary.MailRelay.Presentation/Features/Email/MailRelayMessagesController.cs`

- `GET /api/email/messages/{id:guid}`

### MailRelayProviderEventsController

Source: `MailRelay/FoodDiary.MailRelay.Presentation/Features/Email/MailRelayProviderEventsController.cs`

- `POST /api/email/providers/aws-ses/sns`
- `POST /api/email/providers/mailgun/events`

### MailRelayQueueController

Source: `MailRelay/FoodDiary.MailRelay.Presentation/Features/Email/MailRelayQueueController.cs`

- `GET /api/email/queue/stats`
- `POST /api/email/send`

### MailRelaySuppressionsController

Source: `MailRelay/FoodDiary.MailRelay.Presentation/Features/Email/MailRelaySuppressionsController.cs`

- `GET /api/email/suppressions`
- `POST /api/email/suppressions`
- `DELETE /api/email/suppressions/{email}`

## Focused Tests

- `tests/FoodDiary.Application.Tests/Authentication/EmailSenderTests.cs`
- `tests/FoodDiary.Domain.Tests/Domain/EmailTemplateInvariantTests.cs`
- `tests/FoodDiary.Infrastructure.Tests/Persistence/EmailOutboxTests.cs`
- `tests/FoodDiary.Infrastructure.Tests/Services/EmailSenderTests.cs`
- `tests/FoodDiary.Infrastructure.Tests/Services/EmailTemplateProviderTests.cs`
- `tests/FoodDiary.Presentation.Api.Tests/EmailVerificationNotifierTests.cs`

## Working Rule

Use this page for discovery only. Read the nearest scoped `AGENTS.md` and
verify behavior in source code, tests, and API contract snapshots before
changing the module.
