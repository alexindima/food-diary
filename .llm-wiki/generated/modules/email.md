---
id: generated.module.email
kind: module
status: current
generated_by: .llm-wiki/tools/Build-LlmWikiModulePages.ps1
sources:
  - .llm-wiki/tools/Build-LlmWikiModulePages.ps1
  - .llm-wiki/generated/repository-catalog.json
  - docs/architecture/module-dependencies.json
  - docs/architecture/backend-modules.json
---

# Email

## Graph

- Origin: module-graph
- Business-module dependencies: none observed
- Abstraction-contract dependencies: Admin
- Business-module consumers: Admin, Authentication
- Host/adapter consumers: FoodDiary.Integrations, FoodDiary.JobManager
- Evidence model: compile-time namespaces plus project/composition source evidence; runtime DI/reflection may be incomplete.

## Source Areas

- `FoodDiary.Application.Abstractions/Email`
- `FoodDiary.Application/Email`
- `FoodDiary.Infrastructure/Persistence/Configurations/Email`
- `FoodDiary.Infrastructure/Persistence/Email`

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

## Boundary Health

- Role: aggregate-owner
- Physical isolation: folder
- Architecture guardrails: graph-only
- Declared owned entities: not yet enumerated
- Public contract files: 3
- Observed external consumer groups: 4
- Foreign repositories acquired: guarded where enforcement is explicit; otherwise not inferred from this page

## Public Surface

- Public contract types: 3
- Exported repository-shaped contracts: 0
- `interface IEmailOutbox`
- `interface IEmailOutboxProcessor`
- `interface IEmailTransport`

## Focused Tests

Test paths below are discovery evidence, not proof that a boundary assertion executed or passed.

- [behavioral-or-text-match] `tests/FoodDiary.Application.Tests/Authentication/EmailSenderTests.cs`
- [behavioral-or-text-match] `tests/FoodDiary.Domain.Tests/Domain/EmailTemplateInvariantTests.cs`
- [behavioral-or-text-match] `tests/FoodDiary.Infrastructure.Tests/Persistence/EmailOutboxTests.cs`
- [behavioral-or-text-match] `tests/FoodDiary.Infrastructure.Tests/Services/EmailSenderTests.cs`
- [behavioral-or-text-match] `tests/FoodDiary.Infrastructure.Tests/Services/EmailTemplateProviderTests.cs`
- [presentation] `tests/FoodDiary.Presentation.Api.Tests/EmailVerificationNotifierTests.cs`

## Working Rule

Use this page for discovery only. Read the nearest scoped `AGENTS.md` and
verify behavior in source code, tests, and API contract snapshots before
changing the module.
