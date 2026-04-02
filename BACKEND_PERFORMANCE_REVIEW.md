# Backend Performance Review

## Scope

This document tracks the backend performance baseline, the main query risks, and the guardrails required for production changes.

## Current Query Profile

### Healthy areas

- Most read-heavy repositories already use `AsNoTracking()`.
- High-fanout object graphs in `MealRepository` and `RecipeRepository` already use `AsSplitQuery()`.
- Existing indexes cover core auth and tracking paths such as `Users.Email`, `Users.TelegramUserId`, `RecentItems`, `HydrationEntries`, `WeightEntries`, `WaistEntries`, and `AiUsage`.

### Main risks identified

1. `ProductRepository.GetPagedAsync`
Used case-insensitive search via `ToLower().Contains(...)`, which pushes normalization into the SQL expression and is a poor fit for PostgreSQL query planning.

2. `RecipeRepository.GetPagedAsync`
Had the same search pattern and the same planning risk.

3. `MealRepository.GetPagedAsync`
Filters by `UserId` and optional date range, then sorts by `Date` and `CreatedOnUtc`, but had no explicit composite index for that access path.

4. `ProductRepository` and `RecipeRepository` paging
Both sort by `CreatedOnUtc` after filtering by ownership and public visibility, but had no explicit index coverage for those common read paths.

## Changes Applied In B07

- Replaced `ToLower().Contains(...)` search in product and recipe paging with escaped PostgreSQL `ILIKE`.
- Added composite indexes for:
  - `Products(UserId, CreatedOnUtc)`
  - `Products(Visibility, CreatedOnUtc)`
  - `Recipes(UserId, CreatedOnUtc)`
  - `Recipes(Visibility, CreatedOnUtc)`
  - `Meals(UserId, Date, CreatedOnUtc)`
- Added PostgreSQL integration coverage for escaped `%` search behavior in product and recipe repositories.

## Remaining Gaps

1. No load-test baseline yet for meal, product, and recipe list endpoints.
2. Search still uses `%term%` semantics, so future ranking/relevance needs may eventually justify a dedicated search layer.

## First Regression Gate Added In B12

The first repeatable performance gates are intentionally narrow and PostgreSQL-backed:

- Path: `ProductRepository.GetPagedAsync`
- Scenario: first owned-products page, `page=1`, `limit=25`, no public items, no search term
- Seed: `1500` owned products for one user
- Threshold: second measured execution must complete within `250 ms`
- Test: `ProductRepositoryIntegrationTests.GetPagedAsync_FirstOwnedPage_StaysWithinLatencyBudget`
- Path: `RecipeRepository.GetPagedAsync`
- Scenario: first owned-recipes page, `page=1`, `limit=25`, no public items, no search term
- Seed: `1500` owned recipes for one user
- Threshold: second measured execution must complete within `250 ms`
- Test: `RecipeRepositoryIntegrationTests.GetPagedAsync_FirstOwnedPage_StaysWithinLatencyBudget`

Why this path first:

- product list is a high-value read path
- it was already part of the `B07` query review and indexing pass
- the scenario is simple enough to stay repeatable in Docker-backed PostgreSQL tests without a dedicated benchmark harness

Why recipe was mirrored next:

- recipe paging had the same search and index-risk profile as product paging in `B07`
- the repository shape is close enough that the same latency budget remains understandable and reviewable

How to use the gate:

- run infrastructure integration tests when Docker is available
- treat threshold failures as regression review triggers, not as an excuse to simply raise the budget without analysis
- if the query shape changes intentionally, update both the threshold rationale and this document

## Performance Guardrails

Every backend change that affects query shape should answer:

- Does the hot path remain `AsNoTracking()` when mutation is not required?
- Does a new `Include` introduce cartesian growth?
- Is there an index for the actual `WHERE + ORDER BY` pattern?
- Is pagination bounded?
- If search uses contains-style matching, is PostgreSQL-specific behavior intentional and tested?

## Endpoint Baselines Added

The next regression gates sit one layer higher than the repository checks and exercise the HTTP pipeline against PostgreSQL-backed application state:

- Path: `POST /api/v1/auth/refresh`
- Scenario: refresh token rotation after a warm-up refresh
- Threshold: measured refresh must complete within `350 ms`
- Test: `PostgresPerformanceBaselineTests.Refresh_WithWarmTokenRotation_StaysWithinLatencyBudget`

- Path: `GET /api/v1/products?page=1&limit=25&includePublic=false`
- Scenario: first owned-products page for a user with `1500` private products
- Threshold: second measured execution must complete within `400 ms`
- Test: `PostgresPerformanceBaselineTests.Products_FirstOwnedPage_StaysWithinEndpointLatencyBudget`

- Path: `GET /api/v1/recipes?page=1&limit=25&includePublic=false`
- Scenario: first owned-recipes page for a user with `1500` private recipes
- Threshold: second measured execution must complete within `400 ms`
- Test: `PostgresPerformanceBaselineTests.Recipes_FirstOwnedPage_StaysWithinEndpointLatencyBudget`

- Path: `POST /api/v1/images/upload-url`
- Scenario: authenticated upload-url generation after a warm-up request
- Threshold: measured request must complete within `300 ms`
- Test: `PostgresPerformanceBaselineTests.ImageUploadUrl_WithAuthenticatedUser_StaysWithinLatencyBudget`

- Path: `GET /api/v1/consumptions?page=1&limit=25&dateFrom=2026-03-01&dateTo=2026-03-31`
- Scenario: first page of a monthly meal history range for a user with `1500` seeded meals
- Threshold: second measured execution must complete within `500 ms`
- Test: `PostgresPerformanceBaselineTests.Consumptions_FirstPageWithinMonthRange_StaysWithinEndpointLatencyBudget`

These are intentionally smoke-sized performance baselines:

- they validate the full ASP.NET + MediatR + EF Core path instead of raw repository shape only
- they use warm-up then second measured execution to reduce one-time cold-start noise
- they should be treated as regression tripwires, not microbenchmark truth

## Explain Plan Guards Added

We now also protect the expected index usage for the hottest paging paths with PostgreSQL-backed `EXPLAIN (ANALYZE, BUFFERS, FORMAT JSON)` tests:

- `QueryPlanIntegrationTests.ProductPagingQuery_UsesCompositeOwnershipIndex`
  expects `IX_Products_UserId_CreatedOnUtc`
- `QueryPlanIntegrationTests.RecipePagingQuery_UsesCompositeOwnershipIndex`
  expects `IX_Recipes_UserId_CreatedOnUtc`
- `QueryPlanIntegrationTests.MealPagingQuery_WithDateRange_UsesCompositeOwnershipDateIndex`
  expects `IX_Meals_UserId_Date_CreatedOnUtc`
- `QueryPlanIntegrationTests.ProductSearchQuery_UsesTrigramNameIndex`
  expects `IX_Products_Name`
- `QueryPlanIntegrationTests.RecipeSearchQuery_UsesTrigramNameIndex`
  expects `IX_Recipes_Name`

These tests are intentionally lower-level than the endpoint latency gates:

- they focus on query-plan shape instead of end-to-end request time
- they make index regressions easier to diagnose than raw latency threshold failures
- they should be updated only when the query shape changes intentionally and the new plan is reviewed

## Search Strategy Decision

For now, the chosen strategy is:

- keep escaped PostgreSQL `ILIKE '%term%'` semantics
- back search with `pg_trgm` and GIN trigram indexes on the primary searchable product and recipe text columns
- defer dedicated search redesign unless catalog growth or ranking requirements make trigram-backed `ILIKE` insufficient

## Next Perf Tasks

- add a wider explain review for the full repository OR-based search predicates if those predicates become materially more complex
- consider dedicated search or ranking only if trigram-backed `ILIKE` stops meeting latency or relevance expectations
