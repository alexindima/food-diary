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
- Business-module dependencies: Notifications, Users
- Abstraction-contract dependencies: Notifications, Users
- Business-module consumers: Admin, Billing, Dashboard, Dietologist
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

### AdminSsoController

Source: `Modules/Identity/Presentation/Features/Auth/Controllers/AdminSsoController.cs`

- `POST /api/v{version:apiVersion}/auth/admin-sso/start`
- `POST /api/v{version:apiVersion}/auth/admin-sso/exchange`

### AuthImpersonationController

Source: `Modules/Identity/Presentation/Features/Auth/Controllers/AuthImpersonationController.cs`

- `POST /api/v{version:apiVersion}/auth/impersonation/exchange`

### AuthPasswordController

Source: `Modules/Identity/Presentation/Features/Auth/Controllers/AuthPasswordController.cs`

- `POST /api/v{version:apiVersion}/auth/password-reset/request`
- `POST /api/v{version:apiVersion}/auth/password-reset/confirm`

### AuthSessionController

Source: `Modules/Identity/Presentation/Features/Auth/Controllers/AuthSessionController.cs`

- `POST /api/v{version:apiVersion}/auth/register`
- `POST /api/v{version:apiVersion}/auth/login`
- `POST /api/v{version:apiVersion}/auth/google`
- `POST /api/v{version:apiVersion}/auth/google/link`
- `POST /api/v{version:apiVersion}/auth/refresh`
- `POST /api/v{version:apiVersion}/auth/restore`
- `POST /api/v{version:apiVersion}/auth/verify-email`
- `POST /api/v{version:apiVersion}/auth/verify-email/resend`

### AuthSessionLifecycleController

Source: `Modules/Identity/Presentation/Features/Auth/Controllers/AuthSessionLifecycleController.cs`

- `GET /api/v{version:apiVersion}/auth/sessions`
- `DELETE /api/v{version:apiVersion}/auth/sessions/{sessionId:guid}`
- `DELETE /api/v{version:apiVersion}/auth/sessions`
- `POST /api/v{version:apiVersion}/auth/logout`

### AuthTelegramBackupEmailController

Source: `Modules/Identity/Presentation/Features/Auth/Controllers/AuthTelegramBackupEmailController.cs`

- `POST /api/v{version:apiVersion}/auth/telegram/backup-email/oidc/start`
- `POST /api/v{version:apiVersion}/auth/telegram/backup-email/oidc/complete`

### AuthTelegramController

Source: `Modules/Identity/Presentation/Features/Auth/Controllers/AuthTelegramController.cs`

- `POST /api/v{version:apiVersion}/auth/telegram/disconnect`
- `POST /api/v{version:apiVersion}/auth/telegram/backup-email`
- `POST /api/v{version:apiVersion}/auth/telegram/unlink`
- `POST /api/v{version:apiVersion}/auth/telegram/verify`
- `POST /api/v{version:apiVersion}/auth/telegram/login-widget`
- `POST /api/v{version:apiVersion}/auth/telegram/link`
- `POST /api/v{version:apiVersion}/auth/telegram/bot/auth`

### AuthTelegramOnboardingController

Source: `Modules/Identity/Presentation/Features/Auth/Controllers/AuthTelegramOnboardingController.cs`

- `GET /api/v{version:apiVersion}/auth/telegram/configuration`
- `POST /api/v{version:apiVersion}/auth/telegram/oidc/start`
- `POST /api/v{version:apiVersion}/auth/telegram/oidc/start-link`
- `POST /api/v{version:apiVersion}/auth/telegram/oidc/exchange`
- `POST /api/v{version:apiVersion}/auth/telegram/mini-app/begin`
- `POST /api/v{version:apiVersion}/auth/telegram/mini-app/begin-link`
- `POST /api/v{version:apiVersion}/auth/telegram/complete`
- `POST /api/v{version:apiVersion}/auth/telegram/complete-link`

### AuthTelegramUnlinkController

Source: `Modules/Identity/Presentation/Features/Auth/Controllers/AuthTelegramUnlinkController.cs`

- `POST /api/v{version:apiVersion}/auth/telegram/unlink/oidc/start`
- `POST /api/v{version:apiVersion}/auth/telegram/unlink/oidc/complete`

### BrowserSessionController

Source: `Modules/Identity/Presentation/Features/Auth/Controllers/BrowserSessionController.cs`

- `POST /api/v{version:apiVersion}/auth/session`

### TelegramOperationsController

Source: `Modules/Identity/Presentation/Features/Auth/Controllers/TelegramOperationsController.cs`

- `POST /api/v{version:apiVersion}/auth/telegram/bot/operations`
- `GET /api/v{version:apiVersion}/auth/telegram/bot/operations/ready`
- `POST /api/v{version:apiVersion}/auth/telegram/bot/operations/{operationId:guid}/lease`
- `POST /api/v{version:apiVersion}/auth/telegram/bot/operations/{operationId:guid}/checkpoint`
- `POST /api/v{version:apiVersion}/auth/telegram/bot/operations/{operationId:guid}/resume`

## Boundary Health

- Role: aggregate-owner
- Physical isolation: module-root
- Architecture guardrails: project-reference-matrix-and-module-boundary-tests
- Declared owned entities: EmailTemplate, UserRefreshTokenSession, UserLoginEvent
- Public contract files: 57
- Observed external consumer groups: 7
- Foreign repositories acquired: guarded where enforcement is explicit; otherwise not inferred from this page

## Public Surface

- Public contract types: 57
- Interfaces: 29
- DTO/read-model/projection types: 2
- Enums: 2
- Exported repository-shaped contracts: 13
- Contracts referencing domain entities: 0
- `class JwtImpersonationClaimNames`
- `class JwtSecurityClaimNames`
- `class JwtTokenUseClaimNames`
- `class TelegramIdentityErrors`
- `enum AuthenticationCredentialKind`
- `enum PasswordLoginAttemptOutcome`
- `interface IAdminSsoService`
- `interface IAdminSsoSessionLock`
- `interface IAuthenticationSessionLifetimePolicy`
- `interface IAuthenticationTokenService`
- `interface IEmailTemplateReadModelRepository`
- `interface IEmailTemplateReadRepository`
- `interface IEmailTemplateRepository`
- `interface IEmailTemplateWriteRepository`
- `interface IEmailVerificationNotifier`
- `interface IGoogleTokenValidator`
- `interface IJwtTokenGenerator`
- `interface IPasswordLoginThrottle`
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
- ... 27 more type(s)

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
- [behavioral-or-text-match] `Modules/Identity/tests/FoodDiary.Modules.Identity.Application.Tests/Authentication/AuthenticationCommandHandlerTests.PasswordThrottle.cs`
- [behavioral-or-text-match] `Modules/Identity/tests/FoodDiary.Modules.Identity.Application.Tests/Authentication/AuthenticationCommandHandlerTests.RegisterLogin.cs`
- [behavioral-or-text-match] `Modules/Identity/tests/FoodDiary.Modules.Identity.Application.Tests/Authentication/AuthenticationCommandHandlerTests.ResetRevocation.cs`
- [behavioral-or-text-match] `Modules/Identity/tests/FoodDiary.Modules.Identity.Application.Tests/Authentication/AuthenticationCommandHandlerTests.Telegram.cs`
- [behavioral-or-text-match] `Modules/Identity/tests/FoodDiary.Modules.Identity.Application.Tests/Authentication/AuthenticationCommandHandlerTests.cs`
- [behavioral-or-text-match] `Modules/Identity/tests/FoodDiary.Modules.Identity.Application.Tests/Authentication/AuthenticationTokenServiceTests.cs`
- [behavioral-or-text-match] `Modules/Identity/tests/FoodDiary.Modules.Identity.Application.Tests/Authentication/AuthenticationValidatorsTests.cs`
- [behavioral-or-text-match] `Modules/Identity/tests/FoodDiary.Modules.Identity.Application.Tests/Authentication/BootstrapInitialAdminCommandHandlerTests.cs`
- [behavioral-or-text-match] `Modules/Identity/tests/FoodDiary.Modules.Identity.Application.Tests/Authentication/BrowserSessionRedactionTests.cs`
- [behavioral-or-text-match] `Modules/Identity/tests/FoodDiary.Modules.Identity.Application.Tests/Authentication/BrowserSessionServiceTests.Boundaries.cs`
- [behavioral-or-text-match] `Modules/Identity/tests/FoodDiary.Modules.Identity.Application.Tests/Authentication/BrowserSessionServiceTests.cs`
- [behavioral-or-text-match] `Modules/Identity/tests/FoodDiary.Modules.Identity.Application.Tests/Authentication/CleanupLoginEventsCommandHandlerTests.cs`
- [behavioral-or-text-match] `Modules/Identity/tests/FoodDiary.Modules.Identity.Application.Tests/Authentication/CleanupSessionsCommandHandlerTests.cs`
- [behavioral-or-text-match] `Modules/Identity/tests/FoodDiary.Modules.Identity.Application.Tests/Authentication/DisconnectTelegramCommandTests.cs`
- [behavioral-or-text-match] `Modules/Identity/tests/FoodDiary.Modules.Identity.Application.Tests/Authentication/EmailSenderTests.cs`
- [behavioral-or-text-match] `Modules/Identity/tests/FoodDiary.Modules.Identity.Application.Tests/Authentication/RefreshTokenCommandHandlerTests.cs`
- [behavioral-or-text-match] `Modules/Identity/tests/FoodDiary.Modules.Identity.Application.Tests/Authentication/RegisterCommandValidatorTests.cs`
- [behavioral-or-text-match] `Modules/Identity/tests/FoodDiary.Modules.Identity.Application.Tests/Authentication/SecretInputLimitValidatorTests.cs`
- [behavioral-or-text-match] `Modules/Identity/tests/FoodDiary.Modules.Identity.Application.Tests/Authentication/TelegramAuthenticationBoundaryTests.cs`
- [behavioral-or-text-match] `Modules/Identity/tests/FoodDiary.Modules.Identity.Application.Tests/Authentication/TelegramBackupEmailHandlersTests.cs`
- [behavioral-or-text-match] `Modules/Identity/tests/FoodDiary.Modules.Identity.Application.Tests/Authentication/TelegramBackupEmailServiceTests.cs`
- [behavioral-or-text-match] `Modules/Identity/tests/FoodDiary.Modules.Identity.Application.Tests/Authentication/TelegramOidcValueFormattingTests.cs`

## Working Rule

Use this page for discovery only. Read the nearest scoped `AGENTS.md` and
verify behavior in source code, tests, and API contract snapshots before
changing the module.
