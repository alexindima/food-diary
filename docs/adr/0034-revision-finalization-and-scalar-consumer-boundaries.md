# ADR 0034: Revision finalization and scalar consumer boundaries

- Status: Accepted
- Date: 2026-09-06
- Extends: ADR 0033

## Decision

An old achievement evaluation may fail after a producer has reset the same row for a new revision. LockedBy alone cannot distinguish these requests. Revision and LockedBy are therefore EF concurrency tokens for both success and failure finalization. After a conflict, Gamification discards stale tracking and conditionally releases only a changed revision still owned by the claimed worker. It preserves the new request's timestamps, attempts and error state. A replaced lease remains untouched. PostgreSQL uses one conditional UPDATE by primary key; the InMemory fallback reloads and uses the same concurrency checks. Shared processing owns saves, independent finalization timeout and logging after durable finalization. Dispatch remains at-least-once.

Admin password reset consumes the existing IUserSessionRevocationService instead of Identity's session write repository. Identity retains its existing scoped implementation and persistence semantics.

Admin Contracts owns ExchangeAdminImpersonationCommand for Identity Presentation. Marketing Contracts owns the attribution summary query and its three immutable models for Admin Presentation. Fasting's existing Contracts owns the telemetry summary query and two models. Handlers and validators stay in Application. Application and Presentation cannot reference foreign whole Application projects.

Meals Domain.Contracts owns four IDs and five enums; Favorites Domain.Contracts owns three IDs. Their namespaces, numeric values and conversions remain unchanged. Favorites and MealPlanning use the scalar Meals seam. Meals consumer contracts have no Favorites dependencies. Aggregate state, domain events and entity behavior remain in Domain. Direct references record actual type use.

## Compatibility and rollout

No route, authorization, payload, status code or persisted scalar value changes. No SQL schema migration or backfill is needed: Revision already exists. The EF snapshot records concurrency metadata and model parity is verified. Rebuild and deploy consumers together after type assembly relocation; retire old workers before relying on revision fencing. No deployment or production operation is part of this change.

## Verification

PostgreSQL and InMemory regressions exercise coalescing during success/failure, including the tenth attempt, and stale ownership. Existing retry/dead-letter tests protect unchanged revisions. Admin tests verify revocation on success and absence on denied resets. Architecture tests enforce exact project references, public contract/scalar ownership, foreign Application isolation, persistence capabilities, model parity and acyclic dependency graphs. HTTP integration snapshots protect transport compatibility.
