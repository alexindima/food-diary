# Backend Critical Flow Matrix

Date: 2026-03-28
Scope: PostgreSQL-backed backend critical paths

## Purpose

This document defines the minimum critical backend flows that must remain covered by PostgreSQL-backed integration tests.

It is the execution artifact for `B03` in `BACKEND_10_OF_10_PLAN.md`.

## Rules

- Critical flows must be backed by PostgreSQL, not only EF InMemory.
- A flow may be covered at API level, infrastructure level, or both.
- If a critical flow is not yet covered, it must be marked as `deferred` with a concrete reason.
- Changes that affect a listed flow should update or add the matching test.

## Matrix

| Flow ID | Flow | Level | Current Status | Source |
|---|---|---|---|---|
| CF01 | Register and access protected endpoint | API + Postgres | covered | `tests/FoodDiary.Web.Api.IntegrationTests/PostgresCriticalApiFlowTests.cs` |
| CF02 | Refresh token exchange | API + Postgres | covered | `tests/FoodDiary.Web.Api.IntegrationTests/PostgresCriticalApiFlowTests.cs` |
| CF03 | Request password reset persists reset state | API + Postgres | covered | `tests/FoodDiary.Web.Api.IntegrationTests/PostgresCriticalApiFlowTests.cs` |
| CF04 | Confirm password reset returns fresh authentication | API + Postgres | deferred | No test sender/captured token in current API integration harness |
| CF05 | Delete user then restore account | API + Postgres | covered | `tests/FoodDiary.Web.Api.IntegrationTests/PostgresCriticalApiFlowTests.cs` |
| CF06 | Weight entry duplicate date conflict | API + Postgres | covered | `tests/FoodDiary.Web.Api.IntegrationTests/PostgresCriticalApiFlowTests.cs` |
| CF07 | Waist entry duplicate date conflict | API + Postgres | covered | `tests/FoodDiary.Web.Api.IntegrationTests/PostgresUserFlowTests.cs` |
| CF08 | Product delete preserves shopping-list item text and clears FK | API + Postgres | covered | `tests/FoodDiary.Web.Api.IntegrationTests/PostgresCriticalApiFlowTests.cs` |
| CF09 | Recipe image asset cannot be deleted while referenced | API + Postgres | covered | `tests/FoodDiary.Web.Api.IntegrationTests/PostgresCriticalApiFlowTests.cs` |
| CF10 | Recipe duplicate remains independent after original deletion | API + Postgres | covered | `tests/FoodDiary.Web.Api.IntegrationTests/PostgresUserFlowTests.cs` |
| CF11 | Product -> consumption -> dashboard nutrition path | API + Postgres | covered | `tests/FoodDiary.Web.Api.IntegrationTests/PostgresUserFlowTests.cs` |
| CF12 | Hydration daily aggregation | API + Postgres | covered | `tests/FoodDiary.Web.Api.IntegrationTests/PostgresUserFlowTests.cs` |
| CF13 | User repository loads active user with roles | Infrastructure + Postgres | covered | `tests/FoodDiary.Infrastructure.Tests/Integration/UserRepositoryIntegrationTests.cs` |
| CF14 | User repository paging and search normalization | Infrastructure + Postgres | covered | `tests/FoodDiary.Infrastructure.Tests/Integration/UserRepositoryIntegrationTests.cs` |
| CF15 | Admin dashboard summary counts deleted and premium users correctly | Infrastructure + Postgres | covered | `tests/FoodDiary.Infrastructure.Tests/Integration/UserRepositoryIntegrationTests.cs` |
| CF16 | Cleanup deleted users removes owned data without reassignment | Infrastructure + Postgres | covered | `tests/FoodDiary.Infrastructure.Tests/Integration/UserCleanupServiceIntegrationTests.cs` |
| CF17 | Cleanup deleted users reassigns transferable content correctly | Infrastructure + Postgres | covered | `tests/FoodDiary.Infrastructure.Tests/Integration/UserCleanupServiceIntegrationTests.cs` |

## Current Gaps

### Deferred

- `CF04` Confirm password reset
  Reason: the API integration harness does not yet expose or capture the generated reset token from the email dispatch path.
  Next step: replace `IEmailSender` in a PostgreSQL API factory with a test sender that records password-reset tokens for end-to-end confirmation.

## CI Expectation

At minimum, backend CI should continue running:

- architecture tests
- application tests
- infrastructure tests
- PostgreSQL-backed Web API integration tests for critical paths when Docker is available

## Change Guidance

When a backend change touches a critical flow:

1. Identify the relevant `CFxx` item.
2. Update the existing PostgreSQL-backed test or add a new one.
3. Update this matrix if the flow name, coverage level, or status changes.
