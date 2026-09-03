# Users tests

Own Users-only application and domain tests, including all aggregate partials, roles, goals, state/value objects and lifecycle events. Preserve moved cases; mixed Identity/Admin, host and cross-module tests remain with their established owners. Run ordinary tests without coverage collection and retain real TRX evidence.

DesiredWeightKg, DesiredWaistCm and LanguageCode tests belong to the existing Users Domain suite, which directly references Users Domain.Contracts. No additional contract test project is needed.

Infrastructure.IntegrationTests owns security-state reader provider and scoped DI
tests, reusing only the central PostgreSQL collection/fixture source links and
FoodDiary.Testing. Preserve the relocated version/inactive test; verify missing,
deleted and stale users, cancellation, no tracking and persisted-versus-unsaved
state. Users-only UserRepository tests now belong here. Run both complete
provider suites and API authentication/HTTP consumers; mocks are not SQL evidence.

The same provider project owns administrative read-repository tests. Preserve the
three relocated paging/role/summary cases and cover persisted model mapping,
status semantics, literal LIKE escaping, ordered pages, empty/cancelled reads,
premium counts across account states, and distinct scoped read versus write
adapters. Mixed lookup/write/Identity integration remains central with both adapters.

Keep the seven relocated repository/concurrency/role-membership cases unchanged.
Additional provider tests cover filtered versus inclusive account lookup, exact
issuer/subject matching, tracked identity/goal hydration, staged add/detached update,
role-audit commit/rollback and cancellation. Register all aggregate aliases through
Users in both composition orders; central AddInfrastructure must not own them.
