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
- Extracted project: `Modules/Users/Application/FoodDiary.Modules.Users.Application.csproj`
- Business-module dependencies: none observed
- Abstraction-contract dependencies: Authentication, Images
- Business-module consumers: none observed
- Host/adapter consumers: FoodDiary.JobManager, FoodDiary.Presentation.Api, FoodDiary.Web.Api
- Evidence model: compile-time namespaces plus project/composition source evidence; runtime DI/reflection may be incomplete.

## Source Areas

- `FoodDiary.Application.Abstractions/Users`
- `FoodDiary.Presentation.Api/Features/Users`
- `Modules/Users/Application`
- `Modules/Users/Application/Abstractions`
- `Modules/Users/Contracts`

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
- Physical isolation: module-root-with-shared-persistence-and-identity-seams
- Architecture guardrails: project-reference-matrix-and-module-boundary-tests
- Declared owned entities: User, Role, UserRole, UserRoleAuditEvent, WeightGoal, WaistGoal
- Public contract files: 60
- Observed external consumer groups: 3
- Foreign repositories acquired: guarded where enforcement is explicit; otherwise not inferred from this page

## Public Surface

- Public contract types: 60
- Interfaces: 23
- DTO/read-model/projection types: 33
- Enums: 2
- Exported repository-shaped contracts: 0
- Contracts referencing domain entities: 0
- `class CurrentUserAccessResolver`
- `class UserErrors`
- `enum UserAccountStatusFilter`
- `enum UserPasswordResetIssueStatus`
- `interface ICurrentUserAccessService`
- `interface IProfileDietologistReadService`
- `interface IProfileNotificationReadService`
- `interface IUserAccessTokenSecurityReader`
- `interface IUserAdministrationMutationService`
- `interface IUserAdministrationReadService`
- `interface IUserAiProfileReadService`
- `interface IUserAuthenticationIdentityService`
- `interface IUserAuthenticationRegistrationService`
- `interface IUserBillingService`
- `interface IUserCleanupService`
- `interface IUserCredentialVerificationService`
- `interface IUserCurrentWaistProvider`
- `interface IUserCurrentWeightProvider`
- `interface IUserDashboardProfileReadService`
- `interface IUserDietologistProfileReadService`
- `interface IUserGamificationProfileReadService`
- `interface IUserHydrationProfileReadService`
- `interface IUserNotificationProfileService`
- `interface IUserProfileReadService`
- `interface IUserRoleMembershipService`
- `interface IUserTdeeProfileReadService`
- `interface IUserWeeklyCheckInProfileReadService`
- `record DashboardLayoutModel`
- `record GoalsModel`
- `record ProfileDietologistPermissionsModel`
- ... 30 more type(s)

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
| Users | IUserContextService | Modules | GetAccessibleUserAsync, UpdateUserAsync => Task, Task<Result<User>> | mutation, narrow-read-or-access | migration-required |

## Focused Tests

Test paths below are discovery evidence, not proof that a boundary assertion executed or passed.

- [behavioral-or-text-match] `Modules/Users/tests/FoodDiary.Modules.Users.Application.Tests/Users/AiConsentTests.cs`
- [behavioral-or-text-match] `Modules/Users/tests/FoodDiary.Modules.Users.Application.Tests/Users/CurrentUserAccessPolicyTests.cs`
- [behavioral-or-text-match] `Modules/Users/tests/FoodDiary.Modules.Users.Application.Tests/Users/HistoryPageSummaryHandlerTests.cs`
- [behavioral-or-text-match] `Modules/Users/tests/FoodDiary.Modules.Users.Application.Tests/Users/HistoryProfileCoverageTests.cs`
- [behavioral-or-text-match] `Modules/Users/tests/FoodDiary.Modules.Users.Application.Tests/Users/UpdateUserCommandHandlerTests.cs`
- [behavioral-or-text-match] `Modules/Users/tests/FoodDiary.Modules.Users.Application.Tests/Users/UserApplicationServiceDelegationTests.cs`
- [behavioral-or-text-match] `Modules/Users/tests/FoodDiary.Modules.Users.Application.Tests/Users/UserBillingServiceTests.cs`
- [behavioral-or-text-match] `Modules/Users/tests/FoodDiary.Modules.Users.Application.Tests/Users/UserCredentialVerificationServiceTests.cs`
- [behavioral-or-text-match] `Modules/Users/tests/FoodDiary.Modules.Users.Application.Tests/Users/UserNotificationProfileServiceTests.cs`
- [behavioral-or-text-match] `Modules/Users/tests/FoodDiary.Modules.Users.Application.Tests/Users/UsersDependencyInjectionTests.cs`
- [behavioral-or-text-match] `Modules/Users/tests/FoodDiary.Modules.Users.Application.Tests/Users/UsersFeatureTests.DesiredGoalIdempotency.cs`
- [behavioral-or-text-match] `Modules/Users/tests/FoodDiary.Modules.Users.Application.Tests/Users/UsersFeatureTests.cs`
- [behavioral-or-text-match] `Modules/Users/tests/FoodDiary.Modules.Users.Application.Tests/Users/UsersValidatorTests.cs`
- [behavioral-or-text-match] `Modules/Users/tests/FoodDiary.Modules.Users.Domain.Tests/Domain/ActivityLevelContractTests.cs`
- [behavioral-or-text-match] `Modules/Users/tests/FoodDiary.Modules.Users.Domain.Tests/Domain/MiscDomainInvariantTests.cs`
- [behavioral-or-text-match] `Modules/Users/tests/FoodDiary.Modules.Users.Domain.Tests/Domain/UserGoalAtomicityTests.cs`
- [behavioral-or-text-match] `Modules/Users/tests/FoodDiary.Modules.Users.Domain.Tests/Domain/UserHardeningInvariantTests.cs`
- [behavioral-or-text-match] `Modules/Users/tests/FoodDiary.Modules.Users.Domain.Tests/Domain/UserIdInvariantTests.cs`
- [behavioral-or-text-match] `Modules/Users/tests/FoodDiary.Modules.Users.Domain.Tests/Domain/UserInvariantTests.cs`
- [behavioral-or-text-match] `Modules/Users/tests/FoodDiary.Modules.Users.Domain.Tests/Domain/UserLifecycleEventTests.cs`
- [behavioral-or-text-match] `Modules/Users/tests/FoodDiary.Modules.Users.Domain.Tests/Domain/UserProfileMeasurementInvariantTests.cs`
- [behavioral-or-text-match] `Modules/Users/tests/FoodDiary.Modules.Users.Domain.Tests/Domain/UserRoleAuditInvariantTests.cs`
- [behavioral-or-text-match] `Modules/Users/tests/FoodDiary.Modules.Users.Domain.Tests/Domain/UserRoleDeduplicationTests.cs`
- [behavioral-or-text-match] `Modules/Users/tests/FoodDiary.Modules.Users.Domain.Tests/Domain/UserSecurityCoverageTests.cs`
- [behavioral-or-text-match] `Modules/Users/tests/FoodDiary.Modules.Users.Domain.Tests/Domain/UserSecurityVersionTests.cs`
- [behavioral-or-text-match] `Modules/Users/tests/FoodDiary.Modules.Users.Domain.Tests/Domain/UserValueObjectsInvariantTests.cs`
- [behavioral-or-text-match] `Modules/Users/tests/FoodDiary.Modules.Users.Domain.Tests/Domain/WaistGoalInvariantTests.cs`
- [behavioral-or-text-match] `Modules/Users/tests/FoodDiary.Modules.Users.Domain.Tests/Domain/WeightGoalInvariantTests.cs`
- [behavioral-or-text-match] `Modules/Users/tests/FoodDiary.Modules.Users.Domain.Tests/ValueObjects/AdditionalValueObjectsInvariantTestsDesiredTests.cs`
- [behavioral-or-text-match] `Modules/Users/tests/FoodDiary.Modules.Users.Domain.Tests/ValueObjects/AdditionalValueObjectsInvariantTestsLanguageCodeTests.cs`

## Working Rule

Use this page for discovery only. Read the nearest scoped `AGENTS.md` and
verify behavior in source code, tests, and API contract snapshots before
changing the module.
