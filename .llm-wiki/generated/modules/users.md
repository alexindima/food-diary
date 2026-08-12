---
id: generated.module.users
kind: module
status: current
generated_by: .llm-wiki/tools/Build-LlmWikiModulePages.ps1
sources:
  - .llm-wiki/tools/Build-LlmWikiModulePages.ps1
  - .llm-wiki/generated/repository-catalog.json
  - docs/architecture/module-dependencies.json
  - docs/architecture/backend-modules.json
---

# Users

## Graph

- Origin: extracted-project
- Extracted project: `FoodDiary.Application.Users/FoodDiary.Application.Users.csproj`
- Business-module dependencies: none observed
- Abstraction-contract dependencies: none observed
- Business-module consumers: none observed
- Host/adapter consumers: FoodDiary.Initializer, FoodDiary.JobManager, FoodDiary.Presentation.Api, FoodDiary.Web.Api
- Evidence model: compile-time namespaces plus project/composition source evidence; runtime DI/reflection may be incomplete.

## Source Areas

- `FoodDiary.Application.Abstractions/Users`
- `FoodDiary.Domain/Entities/Users`
- `FoodDiary.Infrastructure/Persistence/Configurations/Users`
- `FoodDiary.Infrastructure/Persistence/Users`
- `FoodDiary.Presentation.Api/Features/Users`

## HTTP Surface

### UserAiConsentController

Source: `FoodDiary.Presentation.Api/Features/Users/UserAiConsentController.cs`

- `POST /api/v{version:apiVersion}/users/ai-consent`
- `DELETE /api/v{version:apiVersion}/users/ai-consent`

### UserOverviewController

Source: `FoodDiary.Presentation.Api/Features/Users/UserOverviewController.cs`

- `GET /api/v{version:apiVersion}/users/overview`

### UsersController

Source: `FoodDiary.Presentation.Api/Features/Users/UsersController.cs`

- `GET /api/v{version:apiVersion}/users/info`
- `PATCH /api/v{version:apiVersion}/users/info`
- `PATCH /api/v{version:apiVersion}/users/preferences/appearance`
- `GET /api/v{version:apiVersion}/users/desired-weight`
- `PUT /api/v{version:apiVersion}/users/desired-weight`
- `GET /api/v{version:apiVersion}/users/desired-waist`
- `PUT /api/v{version:apiVersion}/users/desired-waist`
- `DELETE /api/v{version:apiVersion}/users`

### UsersPasswordController

Source: `FoodDiary.Presentation.Api/Features/Users/UsersPasswordController.cs`

- `PATCH /api/v{version:apiVersion}/users/password`
- `PATCH /api/v{version:apiVersion}/users/password/set`

### WaistGoalsController

Source: `FoodDiary.Presentation.Api/Features/Users/WaistGoalsController.cs`

- `GET /api/v{version:apiVersion}/users/waist-goals`

### WeightGoalsController

Source: `FoodDiary.Presentation.Api/Features/Users/WeightGoalsController.cs`

- `GET /api/v{version:apiVersion}/users/weight-goals`

## Boundary Health

- Role: aggregate-owner
- Physical isolation: assembly
- Architecture guardrails: explicit-boundary-tests
- Declared owned entities: User, Role, UserRole, UserRoleAuditEvent
- Public contract files: 65
- Observed external consumer groups: 4
- Foreign repositories acquired: guarded where enforcement is explicit; otherwise not inferred from this page

## Public Surface

- Public contract types: 65
- Interfaces: 28
- DTO/read-model/projection types: 33
- Enums: 2
- Exported repository-shaped contracts: 6
- Contracts referencing domain entities: 5
- `class CurrentUserAccessResolver`
- `class UserErrors`
- `enum UserAccountStatusFilter`
- `enum UserPasswordResetIssueStatus`
- `interface ICurrentUserAccessService`
- `interface IProfileDietologistReadService`
- `interface IProfileNotificationReadService`
- `interface IUserAdministrationMutationService`
- `interface IUserAdministrationReadService`
- `interface IUserAdminReadModelRepository`
- `interface IUserAdminReadRepository`
- `interface IUserAiProfileReadService`
- `interface IUserAuthenticationIdentityService`
- `interface IUserAuthenticationRegistrationService`
- `interface IUserBillingService`
- `interface IUserCleanupService`
- `interface IUserCurrentWaistProvider`
- `interface IUserCurrentWeightProvider`
- `interface IUserDashboardProfileReadService`
- `interface IUserDietologistProfileReadService`
- `interface IUserGamificationProfileReadService`
- `interface IUserGoogleIdentityRepository`
- `interface IUserHydrationProfileReadService`
- `interface IUserLookupRepository`
- `interface IUserNotificationProfileService`
- `interface IUserProfileReadService`
- `interface IUserRepository`
- `interface IUserRoleCatalogService`
- `interface IUserRoleMembershipService`
- `interface IUserTdeeProfileReadService`
- ... 35 more type(s)

## Extraction Readiness

- Abstraction-owned profile-read consumers: 12 across 3 group(s)
- Implementation-owned IUserContextService consumers: 12 across 1 group(s)
- Consumers receiving the User aggregate: 0
- Consumers with aggregate mutation access: 10
- Composition registrations: 1
- Remaining blocker classes: 2
- Extraction readiness: partial; migrate legacy aggregate/mutation consumers

| Consumer | Contract | Owning assembly | Methods/data | Access | Extraction |
| --- | --- | --- | --- | --- | --- |
| Users | IUserContextService | FoodDiary.Application.Users | GetAccessibleUserAsync, UpdateUserAsync => Task, Task<Result<User>> | mutation, narrow-read-or-access | migration-required |

## Focused Tests

Test paths below are discovery evidence, not proof that a boundary assertion executed or passed.

- [behavioral-or-text-match] `tests/FoodDiary.Application.Tests/Users/AiConsentTests.cs`
- [behavioral-or-text-match] `tests/FoodDiary.Application.Tests/Users/CurrentUserAccessPolicyTests.cs`
- [behavioral-or-text-match] `tests/FoodDiary.Application.Tests/Users/HistoryPageSummaryHandlerTests.cs`
- [behavioral-or-text-match] `tests/FoodDiary.Application.Tests/Users/HistoryProfileCoverageTests.cs`
- [behavioral-or-text-match] `tests/FoodDiary.Application.Tests/Users/UpdateUserCommandHandlerTests.cs`
- [behavioral-or-text-match] `tests/FoodDiary.Application.Tests/Users/UserApplicationServiceDelegationTests.cs`
- [behavioral-or-text-match] `tests/FoodDiary.Application.Tests/Users/UserBillingServiceTests.cs`
- [behavioral-or-text-match] `tests/FoodDiary.Application.Tests/Users/UsersFeatureTests.cs`
- [behavioral-or-text-match] `tests/FoodDiary.Application.Tests/Users/UsersValidatorTests.cs`
- [architecture-boundary] `tests/FoodDiary.ArchitectureTests/UsersModuleExtractionTests.cs`
- [presentation] `tests/FoodDiary.Presentation.Api.Tests/UsersControllerTests.cs`
- [presentation] `tests/FoodDiary.Presentation.Api.Tests/UsersPasswordControllerTests.cs`

## Working Rule

Use this page for discovery only. Read the nearest scoped `AGENTS.md` and
verify behavior in source code, tests, and API contract snapshots before
changing the module.
