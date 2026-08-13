# ADR 0010: Meals Terminology and Application Boundary

- Status: Accepted
- Date: 2026-08-13
- Owners: Meal Diary
- Related: ADR 0001, ADR 0004, ADR 0005, ADR 0009
- Supersedes: None

## Context

The meal diary used `Meal` in the domain and persistence layers but `Consumption` in application use cases, HTTP contracts, and parts of the Angular client. The mixed vocabulary obscured ownership and made the capability harder to extract from the core Application project.

## Decision Drivers

- Use one ubiquitous term across domain, application, HTTP, tests, and frontend code.
- Give the Meal Diary an enforceable physical application boundary.
- Keep aggregate-loading repositories distinct from optimized read projections.
- Avoid carrying a permanent compatibility alias for an API that is changed together with all known consumers.

## Considered Options

1. Keep `Consumption` as the external and application term while retaining `Meal` internally.
2. Introduce `/meals` and `Meal` names while temporarily preserving aliases for the old contract.
3. Perform a coordinated hard cut to `Meal`/`Meals` across all current consumers.

## Decision

Use `Meal` and `Meals` as the canonical terminology. The application capability lives in `FoodDiary.Application.Meals`, and its public HTTP base route is `/api/v1/meals`. Current source folders, files, namespaces, request/response types, tests, localization keys, and client configuration must not use the former term.

The change is a coordinated breaking contract migration with no legacy route alias. Existing domain entities and database mappings already use `Meal`, so no data migration is required. Aggregate-loading persistence uses `IMealReadRepository`; optimized application projections use `IMealProjectionReadRepository` and `MealProjectionReadModel`.

Shared manual-nutrition limits used by both Meals and Recipes live in `FoodDiary.Application.Abstractions/Nutrition` and do not constitute a standalone Nutrition application module.

## Consequences

### Positive

- One term describes the capability across every active layer.
- The module boundary is visible and enforced through project references.
- Read projections cannot be confused with aggregate-loading repositories.

### Negative

- Clients deployed independently from this repository must switch from `/consumptions` to `/meals` in the same release window.
- The coordinated rename creates a large one-time source and snapshot diff.

## Enforcement

- `MealsModuleExtractionTests` protects physical extraction and composition registration.
- `ProjectDependencyMatrixTests` constrains project references.
- API integration tests and OpenAPI snapshots protect `/api/v1/meals`.
- Repository searches and review policy reject the former term in current source paths and content.

## Follow-up

- Deploy the API and bundled Angular client together and monitor meal create/read/update/repeat/delete failures after release.
