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
- Abstraction-contract dependencies: Authentication
- Business-module consumers: none observed
- Host/adapter consumers: FoodDiary.JobManager, FoodDiary.Web.Api
- Evidence model: compile-time namespaces plus project/composition source evidence; runtime DI/reflection may be incomplete.

## Source Areas

- `Modules/Users/Application`
- `Modules/Users/Application/Abstractions`
- `Modules/Users/Contracts`
- `Modules/Users/Presentation`

## HTTP Surface

No literal attribute-routed controller was associated with this module.
## Boundary Health

- Role: aggregate-owner
- Physical isolation: module-root
- Architecture guardrails: project-reference-matrix-and-module-boundary-tests
- Declared owned entities: User, Role, UserRole, UserRoleAuditEvent, WeightGoal, WaistGoal
- Public contract files: 71
- Observed external consumer groups: 2
- Foreign repositories acquired: guarded where enforcement is explicit; otherwise not inferred from this page

## Public Surface

- Public contract types: 71
- Interfaces: 33
- DTO/read-model/projection types: 33
- Enums: 2
- Exported repository-shaped contracts: 6
- Contracts referencing domain entities: 5
- `class CurrentUserAccessResolver`
- `class UserErrors`
- `class UserIdParser`
- `enum UserAccountStatusFilter`
- `enum UserPasswordResetIssueStatus`
- `interface ICurrentUserAccessService`
- `interface IProfileDietologistReadService`
- `interface IProfileNotificationReadService`
- `interface IUserAccessTokenSecurityReader`
- `interface IUserAdministrationMutationService`
- `interface IUserAdministrationReadService`
- `interface IUserAdminReadModelRepository`
- `interface IUserAdminReadRepository`
- `interface IUserAiProfileReadService`
- `interface IUserAuthenticationIdentityService`
- `interface IUserAuthenticationRegistrationService`
- `interface IUserBillingService`
- `interface IUserCleanupService`
- `interface IUserCredentialVerificationService`
- `interface IUserCurrentWaistProvider`
- `interface IUserCurrentWeightProvider`
- `interface IUserDashboardProfileReadService`
- `interface IUserDataPurgeParticipant`
- `interface IUserDietologistProfileReadService`
- `interface IUserGamificationProfileReadService`
- `interface IUserGoogleIdentityRepository`
- `interface IUserHydrationProfileReadService`
- `interface IUserLookupRepository`
- `interface IUserNotificationProfileService`
- `interface IUserProfileImageService`
- ... 41 more type(s)

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

- [behavioral-or-text-match] `Modules/Users/tests/FoodDiary.Modules.Users.Application.Tests/FeatureErrorContractTests.cs`
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

## Working Rule

Use this page for discovery only. Read the nearest scoped `AGENTS.md` and
verify behavior in source code, tests, and API contract snapshots before
changing the module.
