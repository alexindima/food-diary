# Dashboard ownership inventory

Dashboard is a read composer without owned aggregates or EF mappings. Application
owns snapshots, builders, validation and its scoped statistics adapter over Meals.
Contracts owns snapshot DTOs and the client-dashboard query. General nutrition
requests/buckets belong to Meals; Statistics and WeeklyCheckIn dispatch them directly.

No Dashboard.Infrastructure assembly remains. Hosts use AddDashboardModule and
AddReadModelComposition; the latter owns body/meal SQL readers and scoped aliases.
HTTP routes and wire contracts retain their existing Presentation owners.
Application tests remain module-owned. Cross-module projection tests live in
Platform/tests/FoodDiary.Infrastructure.Tests/Persistence/Dashboard. Existing
PostgreSQL, host and API suites retain their owners and scenarios. No schema or
HTTP change is intended. See ADR 0049 for the decision and verification obligations.
