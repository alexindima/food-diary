# Users tests

Own Users-only application and domain tests, including all aggregate partials, roles, goals, state/value objects and lifecycle events. Preserve moved cases; mixed Identity/Admin, host and cross-module tests remain with their established owners. Run ordinary tests without coverage collection and retain real TRX evidence.

DesiredWeightKg, DesiredWaistCm and LanguageCode tests belong to the existing Users Domain suite, which directly references Users Domain.Contracts. No additional contract test project is needed.

Infrastructure.IntegrationTests owns security-state reader provider and scoped DI
tests, reusing only the central PostgreSQL collection/fixture source links and
FoodDiary.Testing. Preserve the relocated version/inactive test; verify missing,
deleted and stale users, cancellation, no tracking and persisted-versus-unsaved
state. The remaining mixed UserRepository tests stay central. Run both complete
provider suites and API authentication/HTTP consumers; mocks are not SQL evidence.
