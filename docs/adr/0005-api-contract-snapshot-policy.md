# ADR 0005: API Contract Snapshot Policy

## Status
Accepted

## Context
FoodDiary exposes HTTP APIs consumed by the Angular client, admin app, Telegram integration, and internal service clients. Unintentional changes to routes, payloads, status codes, or OpenAPI output can break clients.

## Decision
Treat API/OpenAPI snapshots under `tests/FoodDiary.Web.Api.IntegrationTests/Snapshots/` as contract artifacts.

If a backend HTTP contract changes intentionally:
- update the relevant integration snapshots,
- commit snapshot changes with the feature,
- mention the contract change in review/commit context.

## Consequences
Benefits:
- Contract drift is visible in diffs.
- API changes are reviewed intentionally.
- Agents have a concrete place to verify expected HTTP surface changes.

Tradeoffs:
- Intentional API changes require snapshot maintenance.
- Snapshot updates should not be used to hide accidental regressions.
