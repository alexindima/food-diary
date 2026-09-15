# BodyMetrics Tests

Use canonical FoodDiary.Modules.BodyMetrics.<Layer>.Tests assembly names and project-relative namespaces.

Keep focused BodyMetrics application, domain, and infrastructure suites here. Preserve domain invariant coverage when moving donor tests and avoid duplicate cases. Shared DbContext, migration, HTTP, host, Dashboard, cleanup, goal lifecycle, and cross-module scenarios stay with their proven owners.

In-memory repositories implement repository ports only. Compose real ReadWeight*/ReadWaist* handlers with RequestTestSender.Create for query feature tests; do not duplicate request dispatch, mapping or aggregation in repository doubles.
