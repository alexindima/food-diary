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
- Host/adapter consumers: FoodDiary.Initializer, FoodDiary.JobManager, FoodDiary.Web.Api
- Evidence model: compile-time namespaces plus project/composition source evidence; runtime DI/reflection may be incomplete.

## Source Areas

- `Modules/Users/Application`
- `Modules/Users/Application.Abstractions`
- `Modules/Users/Contracts`
- `Modules/Users/PersistenceModel`
- `Modules/Users/Presentation`

## HTTP Surface

No literal attribute-routed controller was associated with this module.
## Boundary Health

- Role: aggregate-owner
- Physical isolation: module-root
- Architecture guardrails: project-reference-matrix-and-module-boundary-tests
- Declared owned entities: User, Role, UserRole, UserRoleAuditEvent, WeightGoal, WaistGoal
- Public contract files: 90
- Observed external consumer groups: 3
- Foreign repositories acquired: guarded where enforcement is explicit; otherwise not inferred from this page

## Public Surface

- Public contract types: 90
- Interfaces: 34
- DTO/read-model/projection types: 36
- Enums: 2
- Exported repository-shaped contracts: 7
- Contracts referencing domain entities: 0
- `class CurrentUserAccessResolver`
- `class UserAuthenticationErrors`
- `class UserErrors`
- `class UserIdParser`
- `enum UserAccountStatusFilter`
- `enum UserPasswordResetIssueStatus`
- `interface ICurrentUserAccessService`
- `interface IProfileDietologistReadService`
- `interface IProfileNotificationReadService`
- `interface IUserAccessTokenSecurityReader`
- `interface IUserAdminReadModelRepository`
- `interface IUserAdminReadRepository`
- `interface IUserAiProfileReadService`
- `interface IUserAuthenticationIdentityService`
- `interface IUserAuthenticationRegistrationService`
- `interface IUserBillingProfileReadModelRepository`
- `interface IUserCleanupService`
- `interface IUserCommentAuthorReadService`
- `interface IUserCredentialVerificationService`
- `interface IUserCurrentWaistProvider`
- `interface IUserCurrentWeightProvider`
- `interface IUserDashboardProfileReadService`
- `interface IUserDataPurgeParticipant`
- `interface IUserDietologistProfileReadService`
- `interface IUserFastingReminderReadService`
- `interface IUserGamificationProfileReadService`
- `interface IUserGoogleIdentityRepository`
- `interface IUserHydrationProfileReadService`
- `interface IUserLookupRepository`
- `interface IUserNotificationProfileService`
- ... 60 more type(s)

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

- [behavioral-or-text-match] `Modules/Users/tests/FoodDiary.Modules.Users.Application.Tests/AiConsentTests.cs`
- [behavioral-or-text-match] `Modules/Users/tests/FoodDiary.Modules.Users.Application.Tests/Authentication/SecretInputLimitValidatorTests.cs`
- [behavioral-or-text-match] `Modules/Users/tests/FoodDiary.Modules.Users.Application.Tests/CurrentUserAccessPolicyTests.cs`
- [behavioral-or-text-match] `Modules/Users/tests/FoodDiary.Modules.Users.Application.Tests/FeatureErrorContractTests.cs`
- [behavioral-or-text-match] `Modules/Users/tests/FoodDiary.Modules.Users.Application.Tests/HistoryProfileCoverageTests.cs`
- [behavioral-or-text-match] `Modules/Users/tests/FoodDiary.Modules.Users.Application.Tests/Support/AllowImageAssetAccessService.cs`
- [behavioral-or-text-match] `Modules/Users/tests/FoodDiary.Modules.Users.Application.Tests/Support/RecordingImageAssetAccessService.cs`
- [behavioral-or-text-match] `Modules/Users/tests/FoodDiary.Modules.Users.Application.Tests/TelegramIdentityRecoveryTests.cs`
- [behavioral-or-text-match] `Modules/Users/tests/FoodDiary.Modules.Users.Application.Tests/UpdateUserCommandHandlerTests.cs`
- [behavioral-or-text-match] `Modules/Users/tests/FoodDiary.Modules.Users.Application.Tests/UserApplicationServiceDelegationTests.cs`
- [behavioral-or-text-match] `Modules/Users/tests/FoodDiary.Modules.Users.Application.Tests/UserBillingServiceTests.cs`
- [behavioral-or-text-match] `Modules/Users/tests/FoodDiary.Modules.Users.Application.Tests/UserCredentialVerificationServiceTests.cs`
- [behavioral-or-text-match] `Modules/Users/tests/FoodDiary.Modules.Users.Application.Tests/UserNotificationProfileServiceTests.cs`
- [behavioral-or-text-match] `Modules/Users/tests/FoodDiary.Modules.Users.Application.Tests/UserRepositoryDefaultMethodTests.cs`
- [behavioral-or-text-match] `Modules/Users/tests/FoodDiary.Modules.Users.Application.Tests/UserTelegramAccountServiceTests.cs`
- [behavioral-or-text-match] `Modules/Users/tests/FoodDiary.Modules.Users.Application.Tests/UsersDependencyInjectionTests.cs`
- [behavioral-or-text-match] `Modules/Users/tests/FoodDiary.Modules.Users.Application.Tests/UsersFeatureTests.DesiredGoalIdempotency.cs`
- [behavioral-or-text-match] `Modules/Users/tests/FoodDiary.Modules.Users.Application.Tests/UsersFeatureTests.cs`
- [behavioral-or-text-match] `Modules/Users/tests/FoodDiary.Modules.Users.Application.Tests/UsersValidatorTests.cs`
- [behavioral-or-text-match] `Modules/Users/tests/FoodDiary.Modules.Users.Domain.Tests/ActivityLevelContractTests.cs`
- [behavioral-or-text-match] `Modules/Users/tests/FoodDiary.Modules.Users.Domain.Tests/MiscDomainInvariantTests.cs`
- [behavioral-or-text-match] `Modules/Users/tests/FoodDiary.Modules.Users.Domain.Tests/TelegramAccountTests.cs`
- [behavioral-or-text-match] `Modules/Users/tests/FoodDiary.Modules.Users.Domain.Tests/UserGoalAtomicityTests.cs`
- [behavioral-or-text-match] `Modules/Users/tests/FoodDiary.Modules.Users.Domain.Tests/UserHardeningInvariantTests.cs`
- [behavioral-or-text-match] `Modules/Users/tests/FoodDiary.Modules.Users.Domain.Tests/UserIdInvariantTests.cs`
- [behavioral-or-text-match] `Modules/Users/tests/FoodDiary.Modules.Users.Domain.Tests/UserInvariantTests.cs`
- [behavioral-or-text-match] `Modules/Users/tests/FoodDiary.Modules.Users.Domain.Tests/UserLifecycleEventTests.cs`
- [behavioral-or-text-match] `Modules/Users/tests/FoodDiary.Modules.Users.Domain.Tests/UserProfileMeasurementInvariantTests.cs`
- [behavioral-or-text-match] `Modules/Users/tests/FoodDiary.Modules.Users.Domain.Tests/UserRoleAuditInvariantTests.cs`
- [behavioral-or-text-match] `Modules/Users/tests/FoodDiary.Modules.Users.Domain.Tests/UserRoleDeduplicationTests.cs`

## Working Rule

Use this page for discovery only. Read the nearest scoped `AGENTS.md` and
verify behavior in source code, tests, and API contract snapshots before
changing the module.
