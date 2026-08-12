---
id: generated.module.authentication
kind: module
status: current
generated_by: .llm-wiki/tools/Build-LlmWikiModulePages.ps1
sources:
  - .llm-wiki/tools/Build-LlmWikiModulePages.ps1
  - .llm-wiki/generated/repository-catalog.json
  - docs/architecture/module-dependencies.json
  - docs/architecture/backend-modules.json
---

# Authentication

## Graph

- Origin: module-graph
- Business-module dependencies: Email, Notifications
- Abstraction-contract dependencies: Email, Notifications, Users
- Business-module consumers: Admin
- Host/adapter consumers: FoodDiary.Initializer, FoodDiary.Integrations, FoodDiary.JobManager, FoodDiary.Presentation.Api, FoodDiary.Web.Api
- Evidence model: compile-time namespaces plus project/composition source evidence; runtime DI/reflection may be incomplete.

## Source Areas

- `FoodDiary.Application.Abstractions/Authentication`
- `FoodDiary.Application/Authentication`
- `FoodDiary.Infrastructure/Persistence/Authentication`
- `FoodDiary.Infrastructure/Persistence/Configurations/Authentication`

## HTTP Surface

No literal attribute-routed controller was associated with this module.
## Boundary Health

- Role: aggregate-owner
- Physical isolation: folder
- Architecture guardrails: graph-only
- Declared owned entities: not yet enumerated
- Public contract files: 31
- Observed external consumer groups: 6
- Foreign repositories acquired: guarded where enforcement is explicit; otherwise not inferred from this page

## Public Surface

- Public contract types: 31
- Interfaces: 16
- DTO/read-model/projection types: 2
- Enums: 0
- Exported repository-shaped contracts: 6
- Contracts referencing domain entities: 3
- `class JwtImpersonationClaimNames`
- `interface IAdminSsoService`
- `interface IAuthenticationTokenService`
- `interface IEmailSender`
- `interface IEmailTemplateProvider`
- `interface IGoogleTokenValidator`
- `interface IJwtTokenGenerator`
- `interface IPasswordHasher`
- `interface IRefreshTokenSessionReadRepository`
- `interface IRefreshTokenSessionRepository`
- `interface IRefreshTokenSessionWriteRepository`
- `interface ITelegramAssertionReplayGuard`
- `interface ITelegramAuthValidator`
- `interface ITelegramLoginWidgetValidator`
- `interface IUserLoginEventReadRepository`
- `interface IUserLoginEventRepository`
- `interface IUserLoginEventWriteRepository`
- `record AccountCreatedMessage`
- `record AdminSsoCode`
- `record AuthenticationClientContext`
- `record EmailTemplateContent`
- `record EmailVerificationMessage`
- `record GoogleIdentityPayload`
- `record IssuedAuthenticationTokens`
- `record JwtImpersonationContext`
- `record PasswordResetMessage`
- `record TelegramInitData`
- `record TelegramLoginWidgetData`
- `record TestEmailMessage`
- `record UserLoginDeviceSummaryModel`
- ... 1 more type(s)

## Focused Tests

Test paths below are discovery evidence, not proof that a boundary assertion executed or passed.

- [behavioral-or-text-match] `tests/FoodDiary.Application.Tests/Authentication/AuthenticationAdditionalValidatorTests.cs`
- [behavioral-or-text-match] `tests/FoodDiary.Application.Tests/Authentication/AuthenticationCommandHandlerTests.AdminSso.cs`
- [behavioral-or-text-match] `tests/FoodDiary.Application.Tests/Authentication/AuthenticationCommandHandlerTests.EmailVerification.cs`
- [behavioral-or-text-match] `tests/FoodDiary.Application.Tests/Authentication/AuthenticationCommandHandlerTests.ExternalLogin.cs`
- [behavioral-or-text-match] `tests/FoodDiary.Application.Tests/Authentication/AuthenticationCommandHandlerTests.Password.cs`
- [behavioral-or-text-match] `tests/FoodDiary.Application.Tests/Authentication/AuthenticationCommandHandlerTests.RegisterLogin.cs`
- [behavioral-or-text-match] `tests/FoodDiary.Application.Tests/Authentication/AuthenticationCommandHandlerTests.Telegram.cs`
- [behavioral-or-text-match] `tests/FoodDiary.Application.Tests/Authentication/AuthenticationCommandHandlerTests.cs`
- [behavioral-or-text-match] `tests/FoodDiary.Application.Tests/Authentication/AuthenticationTokenServiceTests.cs`
- [behavioral-or-text-match] `tests/FoodDiary.Application.Tests/Authentication/AuthenticationValidatorsTests.cs`
- [behavioral-or-text-match] `tests/FoodDiary.Application.Tests/Authentication/BootstrapInitialAdminCommandHandlerTests.cs`
- [behavioral-or-text-match] `tests/FoodDiary.Application.Tests/Authentication/EmailSenderTests.cs`
- [behavioral-or-text-match] `tests/FoodDiary.Application.Tests/Authentication/RefreshTokenCommandHandlerTests.cs`
- [behavioral-or-text-match] `tests/FoodDiary.Application.Tests/Authentication/RegisterCommandValidatorTests.cs`
- [behavioral-or-text-match] `tests/FoodDiary.Application.Tests/Authentication/Services/InitialAdminBootstrapServiceTests.cs`
- [behavioral-or-text-match] `tests/FoodDiary.Application.Tests/Authentication/UserAgentParserTests.cs`
- [behavioral-or-text-match] `tests/FoodDiary.Infrastructure.Tests/Authentication/AdminSsoServiceTests.cs`
- [behavioral-or-text-match] `tests/FoodDiary.Infrastructure.Tests/Authentication/JwtTokenGeneratorTests.cs`
- [behavioral-or-text-match] `tests/FoodDiary.Infrastructure.Tests/Authentication/WearableOAuthStateServiceTests.cs`

## Working Rule

Use this page for discovery only. Read the nearest scoped `AGENTS.md` and
verify behavior in source code, tests, and API contract snapshots before
changing the module.
