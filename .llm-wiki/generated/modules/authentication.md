---
id: generated.module.authentication
kind: module
status: current
generated_by: .llm-wiki/tools/Build-LlmWikiModulePages.ps1
sources:
  - .llm-wiki/tools/Build-LlmWikiModulePages.ps1
  - .llm-wiki/generated/repository-catalog.json
  - docs/architecture/module-dependencies.json
---

# Authentication

## Graph

- Origin: module-graph
- Dependencies: Email, Notifications, Users
- Consumers: Admin, Dietologist

## Source Areas

- `FoodDiary.Application.Abstractions/Authentication`
- `FoodDiary.Application/Authentication`
- `FoodDiary.Infrastructure/Authentication`
- `FoodDiary.Infrastructure/Persistence/Authentication`
- `FoodDiary.Infrastructure/Persistence/Configurations/Authentication`
- `FoodDiary.Integrations/Authentication`
- `tests/FoodDiary.Application.Tests/Authentication`
- `tests/FoodDiary.Infrastructure.Tests/Authentication`

## HTTP Surface

No literal attribute-routed controller was associated with this module.
## Focused Tests

- `tests/FoodDiary.Application.Tests/Authentication/AuthenticationAdditionalValidatorTests.cs`
- `tests/FoodDiary.Application.Tests/Authentication/AuthenticationCommandHandlerTests.AdminSso.cs`
- `tests/FoodDiary.Application.Tests/Authentication/AuthenticationCommandHandlerTests.EmailVerification.cs`
- `tests/FoodDiary.Application.Tests/Authentication/AuthenticationCommandHandlerTests.ExternalLogin.cs`
- `tests/FoodDiary.Application.Tests/Authentication/AuthenticationCommandHandlerTests.Password.cs`
- `tests/FoodDiary.Application.Tests/Authentication/AuthenticationCommandHandlerTests.RegisterLogin.cs`
- `tests/FoodDiary.Application.Tests/Authentication/AuthenticationCommandHandlerTests.Telegram.cs`
- `tests/FoodDiary.Application.Tests/Authentication/AuthenticationCommandHandlerTests.cs`
- `tests/FoodDiary.Application.Tests/Authentication/AuthenticationTokenServiceTests.cs`
- `tests/FoodDiary.Application.Tests/Authentication/AuthenticationUserAccessPolicyTests.cs`
- `tests/FoodDiary.Application.Tests/Authentication/AuthenticationValidatorsTests.cs`
- `tests/FoodDiary.Application.Tests/Authentication/BootstrapInitialAdminCommandHandlerTests.cs`
- `tests/FoodDiary.Application.Tests/Authentication/EmailSenderTests.cs`
- `tests/FoodDiary.Application.Tests/Authentication/RefreshTokenCommandHandlerTests.cs`
- `tests/FoodDiary.Application.Tests/Authentication/RegisterCommandValidatorTests.cs`
- `tests/FoodDiary.Application.Tests/Authentication/Services/InitialAdminBootstrapServiceTests.cs`
- `tests/FoodDiary.Application.Tests/Authentication/UserAgentParserTests.cs`
- `tests/FoodDiary.Infrastructure.Tests/Authentication/AdminSsoServiceTests.cs`
- `tests/FoodDiary.Infrastructure.Tests/Authentication/JwtTokenGeneratorTests.cs`
- `tests/FoodDiary.Infrastructure.Tests/Authentication/WearableOAuthStateServiceTests.cs`

## Working Rule

Use this page for discovery only. Read the nearest scoped `AGENTS.md` and
verify behavior in source code, tests, and API contract snapshots before
changing the module.
