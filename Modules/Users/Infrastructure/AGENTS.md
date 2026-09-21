# Users infrastructure

Own separable Users persistence adapters and complete module DI. Use narrow persistence coordination contracts; do not reference central Infrastructure.
Do not absorb Identity repositories or provider services. UserCleanupService coordinates ordered owner-side IUserDataPurgeParticipant extensions inside the shared coordinator's per-user transaction and only mutates Users/UserRoles itself; participants never save or commit.

Own `UserAccessTokenSecurityReader`, scoped through `AddUsersPersistence`. Its
unchanged no-tracking query reads persisted active/deleted/security-version state;
API/Identity still owns token validation/issuance. Do not substitute an already
tracked User or cache state. The Users-owned UserRepository lookup, Google,
and write aliases still share one instance and scoped DbContext.

Own `UserAdministrationReadRepository` with both administrative read aliases on
one scoped adapter. Preserve original paging/status/search/role loading and model
mapping, including legacy entity-returning reads. All reads stay no-tracking;
shared UsersWithRoles query shape is preserved independently from tracked lookup.
Do not merge these aliases back into UserRepository or duplicate the paging engine.

Own the complete tracked UserRepository; its four scoped aliases are registered
here, not by AddInfrastructure. Google issuer/subject lookup reads stored Users
state, not an external provider. Do not add SaveChanges/transactions inside the
adapter: writes and role-audit additions remain part of the caller's unit of work.

UserProfileProjectionService owns persisted no-tracking access checks and AI, Dashboard, Dietologist, Gamification, Hydration, TDEE and WeeklyCheckIn projections. These interfaces must not alias the tracked UserContextService. Preserve active/deleted filters; no credentials or goal collections are materialized for narrow profiles.

UserRelatedDataReadService owns batch comment-author and fasting-reminder reads.
Both scoped contract aliases share this adapter. Unlike access profiles, these
related-data projections intentionally retain all account states, matching the
former joins. Query only requested distinct IDs and scalar columns, no tracking,
saves, caches or transactions; empty input performs no SQL and cancellation is honored.

UserCleanupService delegates successful item saving to IModuleTransactionCoordinator.ExecuteItemAsync, so image-deletion outbox entries tracked in ImagesDbContext commit or roll back with user cleanup. Participants still never save or commit.

Current weight and waist implementations live in host ReadModel.Composition, reading only scalar BodyMetrics values through the existing Users consumer ports. AddUsersPersistence no longer registers these cross-module readers; hosts register AddReadModelComposition. Preserve date/creation ordering and nullable empty results.

UsersDbContext owns User, Role, UserRole, UserRoleAuditEvent, WeightGoal and WaistGoal. Runtime adapters receive only owner sets (and DatabaseFacade for role SQL), synchronizing the live shared transaction before operations. Save through IUnitOfWork. Users saves at priority -100 before the central context and dependent modules, independently of DI resolution order. Its options explicitly include TelegramIdentityConflictInterceptor; provider uniqueness details must remain hidden. UserCleanupService performs owner SQL through UsersDbContext, including the profile-image unlink, and invokes ordered foreign-owner purge participants.

Billing profile reads for webhook/renewal processing use IUserBillingProfileReadModelRepository on UserProfileProjectionService. Read persisted scalar account/role state without tracking, including deleted accounts; do not use cached tracked Users for this capability.

Registration uses IModuleTransactionCoordinator for live transaction synchronization,
without resolving FoodDiaryDbContext. Retain the relational guard, operation
cancellation, save priority -100 and conflict interceptor. Cleanup uses the same transaction coordinator through its distinct batch-item boundary.

Each cleanup retry is owned by ExecuteItemAsync, which resets all registered owner trackers after rollback. Clearing only the central tracker is insufficient: unsaved Images outbox entries must never survive a failed user into the next user transaction.

ExecuteItemAsync preserves the existing batch semantics: eligibility FOR UPDATE inside each attempt, false means commit without save, true means unconditional coordinated save then commit, and failures roll back/reset before the next user. Initial scope inspection rejects caller changes even for an empty batch. Post-commit actions are intentionally left untouched, matching the former null queue boundary. Keep external effects out of item retries. Bind UsersDbContext to the live transaction on every attempt; preserve account ordering, reassignment and cancellation behavior.

All module projects and tests use `FoodDiary.Modules.Users.<Project>` identities and namespaces matching physical folders. Projects are siblings, including Application.Abstractions and PersistenceModel. Namespace changes preserve database schema, historical migration metadata, HTTP payloads and runtime behavior.

Cleanup candidate pages use PostgreSQL keyset ordering by (DeletedAt, UserId). Return the last examined cursor even when every candidate fails; do not paginate by successful deletion count. Rethrow caller cancellation before the generic per-user failure handler. Keep ExecuteItemAsync as the per-user transaction owner.

UserBodyMetricHistoryReadService owns bounded no-tracking weight/waist page profiles (active goal plus latest closed goal) and closed-goal pages. Apply persisted active/deleted user checks, project scalar values, and LIMIT in SQL; never hydrate the User aggregate or its goal collections. Closed pages filter EndedAtUtc by the first request's UTC snapshot and order by StartedAtUtc descending, then Id descending. Closed goals are immutable; goals closed after that snapshot enter only a fresh traversal. Preserve transaction synchronization with UsersDbContext.
