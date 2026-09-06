# Users infrastructure

Own separable Users persistence adapters and complete module DI. Depend on the
central Infrastructure project only for the shared DbContext compatibility seam.
Do not absorb Identity repositories or provider services. UserCleanupService coordinates ordered owner-side IUserDataPurgeParticipant extensions inside its per-user transaction and only mutates Users/UserRoles itself; participants never save or commit.

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
