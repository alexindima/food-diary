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

- Origin: module-graph
- Business-module dependencies: Images
- Abstraction-contract dependencies: Authentication, Images
- Business-module consumers: Admin, Ai, Authentication, Consumptions, ContentReports, Cycles, DailyAdvices, Dashboard, Dietologist, Exercises, Export, Fasting, FavoriteMeals, FavoriteProducts, FavoriteRecipes, Gamification, Hydration, Lessons, MealPlans, Notifications, Products, RecipeComments, RecipeLikes, Recipes, ShoppingLists, Statistics, Tdee, Usda, WaistEntries, Wearables, WeeklyCheckIn, WeeklyGoals, WeightEntries
- Host/adapter consumers: FoodDiary.JobManager, FoodDiary.Presentation.Api
- Evidence model: compile-time namespaces plus project/composition source evidence; runtime DI/reflection may be incomplete.

## Source Areas

- `FoodDiary.Application.Abstractions/Users`
- `FoodDiary.Application/Users`
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
- Physical isolation: folder
- Architecture guardrails: explicit-boundary-tests
- Declared owned entities: User, Role, UserRole, UserRoleAuditEvent
- Public contract files: 44
- Observed external consumer groups: 35
- Foreign repositories acquired: guarded where enforcement is explicit; otherwise not inferred from this page

## Public Surface

- Public contract types: 44
- Interfaces: 20
- DTO/read-model/projection types: 22
- Enums: 1
- Exported repository-shaped contracts: 5
- Contracts referencing domain entities: 4
- `class UserErrors`
- `enum UserAccountStatusFilter`
- `interface ICurrentUserAccessService`
- `interface IUserAdminReadModelRepository`
- `interface IUserAdminReadRepository`
- `interface IUserAiProfileReadService`
- `interface IUserCleanupService`
- `interface IUserCurrentWaistProvider`
- `interface IUserCurrentWeightProvider`
- `interface IUserDashboardProfileReadService`
- `interface IUserDietologistProfileReadService`
- `interface IUserDirectoryService`
- `interface IUserGamificationProfileReadService`
- `interface IUserHydrationProfileReadService`
- `interface IUserLookupRepository`
- `interface IUserProfileReadService`
- `interface IUserRepository`
- `interface IUserRoleCatalogService`
- `interface IUserRoleMembershipService`
- `interface IUserTdeeProfileReadService`
- `interface IUserWeeklyCheckInProfileReadService`
- `interface IUserWriteRepository`
- `record DashboardLayoutModel`
- `record GoalsModel`
- `record ProfileDietologistPermissionsModel`
- `record ProfileDietologistRelationshipModel`
- `record ProfileOverviewModel`
- `record ProfileWebPushSubscriptionModel`
- `record UserAdminReadModel`
- `record UserAiProfileModel`
- ... 14 more type(s)

## Extraction Readiness

- Abstraction-owned profile-read consumers: 12 across 5 group(s)
- Implementation-owned IUserContextService consumers: 18 across 5 group(s)
- Consumers receiving the User aggregate: 0
- Consumers with aggregate mutation access: 14
- Composition registrations: 1
- Remaining blocker classes: 2
- Extraction readiness: partial; migrate legacy aggregate/mutation consumers

| Consumer | Contract | Owning assembly | Methods/data | Access | Extraction |
| --- | --- | --- | --- | --- | --- |
| Authentication | IUserContextService | FoodDiary.Application | UpdateUserAsync => Task | mutation | migration-required |
| FoodDiary.Application | IUserContextService | FoodDiary.Application | constructor/registration only => inherited or unresolved | narrow-read-or-access | migration-required |
| Notifications | IUserContextService | FoodDiary.Application | EnsureCanAccessAsync, GetAccessibleUserAsync, UpdateUserAsync => Task, Task<Result<User>> | mutation | migration-required |
| Users | IUserContextService | FoodDiary.Application | GetAccessibleUserAsync, UpdateUserAsync => Task, Task<Result<User>> | mutation, narrow-read-or-access | migration-required |
| WeeklyGoals | IUserContextService | FoodDiary.Application | constructor/registration only => inherited or unresolved | narrow-read-or-access | migration-required |

## Focused Tests

Test paths below are discovery evidence, not proof that a boundary assertion executed or passed.

- [behavioral-or-text-match] `tests/FoodDiary.Application.Tests/Users/AiConsentTests.cs`
- [behavioral-or-text-match] `tests/FoodDiary.Application.Tests/Users/CurrentUserAccessPolicyTests.cs`
- [behavioral-or-text-match] `tests/FoodDiary.Application.Tests/Users/HistoryPageSummaryHandlerTests.cs`
- [behavioral-or-text-match] `tests/FoodDiary.Application.Tests/Users/HistoryProfileCoverageTests.cs`
- [behavioral-or-text-match] `tests/FoodDiary.Application.Tests/Users/UpdateUserCommandHandlerTests.cs`
- [behavioral-or-text-match] `tests/FoodDiary.Application.Tests/Users/UserApplicationServiceDelegationTests.cs`
- [behavioral-or-text-match] `tests/FoodDiary.Application.Tests/Users/UsersFeatureTests.cs`
- [behavioral-or-text-match] `tests/FoodDiary.Application.Tests/Users/UsersValidatorTests.cs`
- [presentation] `tests/FoodDiary.Presentation.Api.Tests/UsersControllerTests.cs`
- [presentation] `tests/FoodDiary.Presentation.Api.Tests/UsersPasswordControllerTests.cs`

## Working Rule

Use this page for discovery only. Read the nearest scoped `AGENTS.md` and
verify behavior in source code, tests, and API contract snapshots before
changing the module.
