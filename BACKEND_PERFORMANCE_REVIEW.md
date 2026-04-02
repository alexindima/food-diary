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

1. No formal latency budgets yet for critical API endpoints.
2. No automated `EXPLAIN ANALYZE` workflow for high-risk SQL paths.
3. Search still uses `%term%` semantics; for large product and recipe catalogs, PostgreSQL trigram indexes or dedicated search should be considered.
4. No load-test baseline yet for meal, product, and recipe list endpoints.

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

These are intentionally smoke-sized performance baselines:

- they validate the full ASP.NET + MediatR + EF Core path instead of raw repository shape only
- they use warm-up then second measured execution to reduce one-time cold-start noise
- they should be treated as regression tripwires, not microbenchmark truth

## Next Perf Tasks

- Add a meal-list endpoint latency budget for `GET /api/v1/consumptions` on seeded historical data.
- Capture `EXPLAIN ANALYZE` for the heaviest product, recipe, and meal queries on seeded data.
- Decide whether product and recipe search should move to trigram-backed search when catalog size grows.
