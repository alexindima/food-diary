---
id: generated.module.identity
kind: module
status: current
generated_by: .llm-wiki/tools/Build-LlmWikiModulePages.ps1
sources:
  - .llm-wiki/tools/Build-LlmWikiModulePages.ps1
  - .llm-wiki/generated/repository-catalog.json
  - docs/architecture/module-dependencies.json
  - docs/architecture/backend-modules.json
---

# Identity

## Graph

- Origin: extracted-project
- Extracted project: `Modules/Identity/Application/FoodDiary.Modules.Identity.Application.csproj`
- Business-module dependencies: none observed
- Abstraction-contract dependencies: Authentication, Email, Notifications, Users
- Business-module consumers: none observed
- Host/adapter consumers: FoodDiary.Initializer, FoodDiary.JobManager, FoodDiary.Web.Api
- Evidence model: compile-time namespaces plus project/composition source evidence; runtime DI/reflection may be incomplete.

## Source Areas

- `Modules/Identity/Application`
- `Modules/Identity/Application.Abstractions`
- `Modules/Identity/Domain`
- `Modules/Identity/Infrastructure`
- `Modules/Identity/Infrastructure/Providers`
- `Modules/Identity/PersistenceModel`
- `Modules/Identity/Presentation`
- `Modules/Identity/Presentation/Features/Auth`

## HTTP Surface

No literal attribute-routed controller was associated with this module.
## Boundary Health

- Role: aggregate-owner
- Physical isolation: module-root
- Architecture guardrails: project-reference-matrix-and-module-boundary-tests
- Declared owned entities: EmailTemplate, UserRefreshTokenSession, UserLoginEvent
- Public contract files: 40
- Observed external consumer groups: 3
- Foreign repositories acquired: guarded where enforcement is explicit; otherwise not inferred from this page

## Public Surface

- Public contract types: 40
- Interfaces: 26
- DTO/read-model/projection types: 1
- Enums: 0
- Exported repository-shaped contracts: 13
- Contracts referencing domain entities: 0
- `class JwtImpersonationClaimNames`
- `class JwtSecurityClaimNames`
- `class JwtTokenUseClaimNames`
- `class TelegramIdentityErrors`
- `interface IAdminSsoService`
- `interface IAuthenticationTokenService`
- `interface IEmailTemplateReadModelRepository`
- `interface IEmailTemplateReadRepository`
- `interface IEmailTemplateRepository`
- `interface IEmailTemplateWriteRepository`
- `interface IEmailVerificationNotifier`
- `interface IGoogleTokenValidator`
- `interface IJwtTokenGenerator`
- `interface IRefreshTokenSessionReadModelRepository`
- `interface IRefreshTokenSessionReadRepository`
- `interface IRefreshTokenSessionRepository`
- `interface IRefreshTokenSessionWriteRepository`
- `interface ITelegramAssertionReplayGuard`
- `interface ITelegramAuthValidator`
- `interface ITelegramIdentityPolicy`
- `interface ITelegramLoginTicketStore`
- `interface ITelegramLoginWidgetValidator`
- `interface ITelegramOidcProvider`
- `interface ITelegramOidcTokenValidator`
- `interface ITelegramOperationPolicy`
- `interface ITelegramOperationStore`
- `interface IUserLoginEventQuery`
- `interface IUserLoginEventReadRepository`
- `interface IUserLoginEventRepository`
- `interface IUserLoginEventWriteRepository`
- ... 10 more type(s)

## Focused Tests

Test paths below are discovery evidence, not proof that a boundary assertion executed or passed.

- [behavioral-or-text-match] `Modules/Identity/tests/FoodDiary.Modules.Identity.Application.Tests/Authentication/ActiveSessionManagementTests.cs`
- [behavioral-or-text-match] `Modules/Identity/tests/FoodDiary.Modules.Identity.Application.Tests/Authentication/AuthenticationAdditionalValidatorTests.cs`
- [behavioral-or-text-match] `Modules/Identity/tests/FoodDiary.Modules.Identity.Application.Tests/Authentication/AuthenticationCommandHandlerTests.AdminSso.cs`
- [behavioral-or-text-match] `Modules/Identity/tests/FoodDiary.Modules.Identity.Application.Tests/Authentication/AuthenticationCommandHandlerTests.EmailVerification.cs`
- [behavioral-or-text-match] `Modules/Identity/tests/FoodDiary.Modules.Identity.Application.Tests/Authentication/AuthenticationCommandHandlerTests.ExternalLogin.cs`
- [behavioral-or-text-match] `Modules/Identity/tests/FoodDiary.Modules.Identity.Application.Tests/Authentication/AuthenticationCommandHandlerTests.IdentityServiceCoverage.cs`
- [behavioral-or-text-match] `Modules/Identity/tests/FoodDiary.Modules.Identity.Application.Tests/Authentication/AuthenticationCommandHandlerTests.Password.cs`
- [behavioral-or-text-match] `Modules/Identity/tests/FoodDiary.Modules.Identity.Application.Tests/Authentication/AuthenticationCommandHandlerTests.PasswordHashMigration.cs`
- [behavioral-or-text-match] `Modules/Identity/tests/FoodDiary.Modules.Identity.Application.Tests/Authentication/AuthenticationCommandHandlerTests.RegisterLogin.cs`
- [behavioral-or-text-match] `Modules/Identity/tests/FoodDiary.Modules.Identity.Application.Tests/Authentication/AuthenticationCommandHandlerTests.Telegram.cs`
- [behavioral-or-text-match] `Modules/Identity/tests/FoodDiary.Modules.Identity.Application.Tests/Authentication/AuthenticationCommandHandlerTests.cs`
- [behavioral-or-text-match] `Modules/Identity/tests/FoodDiary.Modules.Identity.Application.Tests/Authentication/AuthenticationTokenServiceTests.cs`
- [behavioral-or-text-match] `Modules/Identity/tests/FoodDiary.Modules.Identity.Application.Tests/Authentication/AuthenticationValidatorsTests.cs`
- [behavioral-or-text-match] `Modules/Identity/tests/FoodDiary.Modules.Identity.Application.Tests/Authentication/BootstrapInitialAdminCommandHandlerTests.cs`
- [behavioral-or-text-match] `Modules/Identity/tests/FoodDiary.Modules.Identity.Application.Tests/Authentication/CleanupLoginEventsCommandHandlerTests.cs`
- [behavioral-or-text-match] `Modules/Identity/tests/FoodDiary.Modules.Identity.Application.Tests/Authentication/EmailSenderTests.cs`
- [behavioral-or-text-match] `Modules/Identity/tests/FoodDiary.Modules.Identity.Application.Tests/Authentication/RefreshTokenCommandHandlerTests.cs`
- [behavioral-or-text-match] `Modules/Identity/tests/FoodDiary.Modules.Identity.Application.Tests/Authentication/RegisterCommandValidatorTests.cs`
- [behavioral-or-text-match] `Modules/Identity/tests/FoodDiary.Modules.Identity.Application.Tests/Authentication/TelegramAuthenticationBoundaryTests.cs`
- [behavioral-or-text-match] `Modules/Identity/tests/FoodDiary.Modules.Identity.Application.Tests/Authentication/TelegramBackupEmailHandlersTests.cs`
- [behavioral-or-text-match] `Modules/Identity/tests/FoodDiary.Modules.Identity.Application.Tests/Authentication/TelegramBackupEmailServiceTests.cs`
- [behavioral-or-text-match] `Modules/Identity/tests/FoodDiary.Modules.Identity.Application.Tests/Authentication/TelegramOnboardingTests.cs`
- [behavioral-or-text-match] `Modules/Identity/tests/FoodDiary.Modules.Identity.Application.Tests/Authentication/TelegramOperationServiceTests.cs`
- [behavioral-or-text-match] `Modules/Identity/tests/FoodDiary.Modules.Identity.Application.Tests/Authentication/UnlinkTelegramCommandTests.cs`
- [behavioral-or-text-match] `Modules/Identity/tests/FoodDiary.Modules.Identity.Application.Tests/Authentication/UserAgentParserTests.cs`
- [behavioral-or-text-match] `Modules/Identity/tests/FoodDiary.Modules.Identity.Application.Tests/Authentication/UserAuthenticationRegistrationServiceTests.cs`
- [behavioral-or-text-match] `Modules/Identity/tests/FoodDiary.Modules.Identity.Domain.Tests/EmailTemplateInvariantTests.cs`
- [behavioral-or-text-match] `Modules/Identity/tests/FoodDiary.Modules.Identity.Domain.Tests/UserAuditEventInvariantTests.cs`
- [behavioral-or-text-match] `Modules/Identity/tests/FoodDiary.Modules.Identity.Domain.Tests/UserRefreshTokenSessionInvariantTests.cs`
- [integration] `Modules/Identity/tests/FoodDiary.Modules.Identity.Infrastructure.IntegrationTests/Integration/ActiveSessionProjectionIntegrationTests.cs`

## Working Rule

Use this page for discovery only. Read the nearest scoped `AGENTS.md` and
verify behavior in source code, tests, and API contract snapshots before
changing the module.
