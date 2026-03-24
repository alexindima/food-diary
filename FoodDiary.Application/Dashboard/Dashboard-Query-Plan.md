# Dashboard Query Plan

## Problem

`GET /api/dashboard` previously executed multiple database-backed operations in parallel inside one HTTP request scope.

Examples from [`GetDashboardSnapshotQueryHandler.cs`](./Queries/GetDashboardSnapshot/GetDashboardSnapshotQueryHandler.cs):

- `GetStatistics`
- `GetConsumptions`
- `userRepository.GetByIdAsync(...)`
- `weightEntryRepository.GetEntriesAsync(...)`
- `waistEntryRepository.GetEntriesAsync(...)`
- `GetHydrationDailyTotal`
- `GetDailyAdvice`
- `GetWeightSummaries`
- `GetWaistSummaries`

With the current DI setup, `FoodDiaryDbContext` is registered as scoped in [`FoodDiary.Infrastructure/DependencyInjection.cs`](../FoodDiary.Infrastructure/DependencyInjection.cs). Because of that, parallel EF Core operations inside one request can throw:

- `System.InvalidOperationException: A second operation was started on this context instance before a previous operation completed`

This is not related to an empty database.

## Current Safe Fix

The immediate safe fix is already applied:

- remove `Task.WhenAll(...)` from the dashboard handler
- execute the database-backed operations sequentially

This keeps the request correct with the existing scoped `DbContext`.

## Goal

Keep `dashboard` correct and make it faster without relying on unsafe parallel access to one `DbContext`.

## Recommended Direction

Do not treat the dashboard as a chain of independent MediatR queries.

Instead:

- create a dedicated dashboard read path
- fetch only the fields needed for the screen
- prefer projection queries over loading aggregates
- reduce the number of SQL roundtrips first
- only then consider controlled parallelism

## Plan

### Phase 1. Stabilize the Existing Endpoint

- Keep the sequential flow in [`GetDashboardSnapshotQueryHandler.cs`](./Queries/GetDashboardSnapshot/GetDashboardSnapshotQueryHandler.cs).
- Confirm the endpoint works for:
  - empty database
  - user with meals only
  - user with weight/waist/hydration data
  - user with no daily advice for locale
- Add a regression test for the dashboard query path if there is test coverage around application handlers.

Expected outcome:

- no more `DbContext` concurrency exceptions
- predictable request behavior

### Phase 2. Introduce a Dedicated Read Service

Create a dedicated dashboard read service, for example:

- `FoodDiary.Application/Dashboard/Services/IDashboardReadService.cs`
- `FoodDiary.Infrastructure/.../DashboardReadService.cs`

Responsibility:

- gather all data for dashboard in one specialized read model flow
- avoid nested query-inside-query orchestration through MediatR for this endpoint

`GetDashboardSnapshotQueryHandler` should become thin:

- validate input
- call `IDashboardReadService.GetSnapshotAsync(...)`
- map result to `DashboardSnapshotResponse`

Expected outcome:

- simpler control flow
- easier performance tuning
- fewer hidden database roundtrips

### Phase 3. Replace Aggregate Loading With Projections

For dashboard reads, prefer projection queries:

- daily statistics projection
- weekly calories projection
- meals page projection
- latest user goal/profile fields projection
- latest weight and waist summary projections
- hydration total projection

Avoid:

- loading full aggregates when only summary data is needed
- `Include(...)` chains for data that can be projected directly

Expected outcome:

- less materialization
- smaller SQL payloads
- faster response even without parallelism

### Phase 4. Group Data by Query Shape

The dashboard data naturally splits into a few groups:

- user profile and goals
- meals and meal statistics
- body measurements and trends
- hydration
- advice

The read service should aim for a small number of targeted queries instead of many mediator hops.

A good practical target:

- 3 to 6 focused SQL/EF queries total

Expected outcome:

- lower latency
- easier SQL inspection
- less application-layer orchestration

### Phase 5. Consider Controlled Parallelism Only If Needed

If the endpoint is still too slow after the read-model refactor, then introduce controlled parallelism for truly independent read queries.

Important rule:

- each parallel query must use its own `DbContext`

Recommended mechanism:

- `IDbContextFactory<FoodDiaryDbContext>`

Possible candidates for parallel execution after refactor:

- advice query
- hydration totals query
- weight trend query
- waist trend query

Less attractive candidates:

- queries that share the same tables and compete heavily on the same user/date range
- queries that are already cheap after projection

Expected outcome:

- safe parallel reads
- no shared-context EF exceptions

## What Not to Do

- Do not run `Task.WhenAll(...)` over handlers/repositories that share the same scoped `DbContext`.
- Do not rely on `AsNoTracking()` to fix this issue. It does not solve concurrent use of one context instance.
- Do not change `DbContext` lifetime blindly to work around the exception.
- Do not parallelize the current mediator composition before reducing roundtrips.

## Suggested Implementation Order

1. Keep the sequential handler as the baseline.
2. Measure current `dashboard` latency.
3. Introduce `IDashboardReadService`.
4. Move dashboard data access into projection-based read queries.
5. Re-measure.
6. Only if needed, add `IDbContextFactory<FoodDiaryDbContext>` for selected independent read queries.

## Candidate End State

The likely best end state for this endpoint is:

- one application handler
- one dedicated dashboard read service
- a small set of focused read queries
- optional controlled parallelism with separate contexts

That gives better performance than the current design while remaining safe with EF Core.
