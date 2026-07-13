# ADR 0005: API Contract Snapshot Policy

- Status: Accepted
- Date: 2026-05-21
- Owners: Backend API
- Related: ADR-0003
- Supersedes: None

## Context
FoodDiary exposes HTTP APIs consumed by the Angular client, admin app, Telegram integration, and internal service clients. Unintentional changes to routes, payloads, status codes, or OpenAPI output can break clients.

## Decision Drivers

- HTTP contract drift must be visible during review.
- Intentional API changes must travel with their expected contract artifacts.
- Verification should cover both selected endpoint behavior and the generated OpenAPI surface.

## Considered Options

1. Rely on controller unit tests. Fast, but they do not provide a complete transport contract view.
2. Rely only on generated OpenAPI compatibility tooling. Useful for schema changes, but less direct for application-specific response snapshots.
3. Commit API and OpenAPI snapshots verified by integration tests.

## Decision
Treat API/OpenAPI snapshots under `tests/FoodDiary.Web.Api.IntegrationTests/Snapshots/` as contract artifacts.

If a backend HTTP contract changes intentionally:
- update the relevant integration snapshots,
- commit snapshot changes with the feature,
- mention the contract change in review/commit context.

## Consequences

### Positive

- Contract drift is visible in diffs.
- API changes are reviewed intentionally.
- Agents have a concrete place to verify expected HTTP surface changes.

### Negative

- Intentional API changes require snapshot maintenance.
- Snapshot updates should not be used to hide accidental regressions.

## Enforcement

- `tests/FoodDiary.Web.Api.IntegrationTests/Snapshots/`
- `tests/FoodDiary.Web.Api.IntegrationTests`

## Follow-up

- None.
