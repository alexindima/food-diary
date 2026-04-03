# Backend API Contract Governance

Date: 2026-03-28
Scope: HTTP contract of `FoodDiary.Presentation.Api` and `FoodDiary.Web.Api`

## Purpose

Protect the backend HTTP contract from accidental breaking changes.

This document is the execution artifact for `B05` in `BACKEND_10_OF_10_PLAN.md`.

## Current Contract Protection Layers

### 1. Presentation-Level Conventions

Repository tests already protect transport conventions in `tests/FoodDiary.Presentation.Api.Tests`.

Examples:

- controller conventions
- controller security contract
- request/response mappings
- error mapping behavior

### 2. Error Contract Snapshots

`tests/FoodDiary.Web.Api.IntegrationTests/PresentationBoundaryIntegrationTests.cs` verifies important error payloads against stored snapshots.

Snapshot file:

- `tests/FoodDiary.Web.Api.IntegrationTests/Snapshots/error-contract-snapshots.json`

### 3. Payload Contract Snapshots

`tests/FoodDiary.Web.Api.IntegrationTests/PresentationPayloadContractIntegrationTests.cs` verifies normalized payload shapes for representative API responses.

Snapshot file:

- `tests/FoodDiary.Web.Api.IntegrationTests/Snapshots/payload-contract-snapshots.json`

### 4. OpenAPI Contract Snapshots

`tests/FoodDiary.Web.Api.IntegrationTests/PresentationBoundaryIntegrationTests.cs` verifies generated Swagger/OpenAPI output against stored snapshots.

Snapshot files:

- `tests/FoodDiary.Web.Api.IntegrationTests/Snapshots/openapi-focused-contract.json`
- `tests/FoodDiary.Web.Api.IntegrationTests/Snapshots/openapi-auth-admin-contract.json`
- `tests/FoodDiary.Web.Api.IntegrationTests/Snapshots/openapi-full-contract.json`

## Rules

- API routes, request bodies, response shapes, status codes, and documented error behavior are contract.
- If a backend change alters the HTTP contract, the relevant snapshot tests must fail before merge.
- Snapshot updates are allowed only when the contract change is intentional.
- Intentional contract changes should be called out explicitly in the PR/task closeout.

## Idempotent Write Policy

The repository also treats repeated-write behavior as part of the HTTP contract for selected POST endpoints.

Current critical idempotent POST paths:

- `POST /api/v1/products`
- `POST /api/v1/recipes`
- `POST /api/v1/consumptions`
- `POST /api/v1/images/upload-url`
- `POST /api/v1/auth/refresh`

Expectations:

- clients may send `Idempotency-Key` for these paths
- the backend must keep returning the cached successful response for the same `(user, path, key)` tuple
- idempotency is now opt-in at the presentation layer through `EnableIdempotencyAttribute`; it is no longer a blanket policy for every POST action
- changes to global idempotency behavior should be reviewed like other contract changes, not treated as an invisible infrastructure detail

## When To Update Snapshots

Update snapshots only when the change is intended and reviewed.

Examples:

- new route added
- request or response field added or removed
- status code behavior changed
- auth/admin route contract changed
- error payload shape changed

Do not update snapshots just to make tests green.

## How To Update Snapshots

Use the existing integration tests with the environment variable:

```powershell
$env:UPDATE_CONTRACT_SNAPSHOTS='1'
dotnet test tests/FoodDiary.Web.Api.IntegrationTests/FoodDiary.Web.Api.IntegrationTests.csproj --no-restore /p:UseSharedCompilation=false
Remove-Item Env:UPDATE_CONTRACT_SNAPSHOTS
```

For a narrower update, run a filtered test command instead of the full project.

## Review Expectations

When snapshots change, review:

- route names
- request body presence
- response status codes
- auth-protected vs anonymous endpoints
- new required fields
- removed fields
- error payload semantics

## Minimum Requirement For Backend PRs

If a change affects backend HTTP contract, at least one of these must happen:

1. existing contract snapshots stay green without modification
2. relevant snapshots are intentionally updated and reviewed
3. a new contract snapshot/test is added for the new API surface

Repository process support:

- the repository PR template now includes an `API Contract Review` section so intentional contract changes are called out during review instead of being hidden only inside snapshot diffs

## Notes

- The repo already had the technical mechanism; this document formalizes how to use it.
- Contract governance complements, not replaces, behavior tests and critical PostgreSQL-backed flow tests.
