---
id: module.frontend
kind: module
status: current
sources:
  - FoodDiary.Web.Client/AGENTS.md
  - FoodDiary.Web.Client/src/app/AGENTS.md
  - FoodDiary.Web.Client/src/app/features/AGENTS.md
  - FoodDiary.Web.Client/projects/fooddiary-admin/AGENTS.md
  - FoodDiary.Web.Client/projects/fd-ui-kit/AGENTS.md
  - FoodDiary.Web.Client/projects/fd-tour/AGENTS.md
  - docs/frontend/FRONTEND_ARCHITECTURE.md
  - docs/frontend/API_SDK.md
  - FoodDiary.Web.Client/src/app/shared/api/sdk/products-sdk.ts
  - FoodDiary.Web.Client/src/app/features/products/api/product.service.ts
---

# Frontend

The frontend workspace contains the main Angular application, an admin
application, the `fd-ui-kit` component library, and the `fd-tour` engine.

## Boundaries

- Application features live under `src/app/features/<feature>/`.
- Feature roots are not public import surfaces; import concrete layers.
- Shared models cannot depend on API, UI, or feature-local code.
- Shared API code cannot depend on UI or feature-local code.
- Reusable UI primitives come from `fd-ui-kit`.
- Route files are composition boundaries and must not be imported by feature
  implementation code.

The complete dependency model is in
[`FRONTEND_ARCHITECTURE.md`](../../docs/frontend/FRONTEND_ARCHITECTURE.md).

## Implementation Defaults

New application code uses strict TypeScript, signals, `OnPush`, native template
control flow, lazy feature routes, and SSR-safe browser access. New UI copy must
be updated in both English and Russian locale files.

Before editing, read the root frontend guide and the nearest applicable scoped
guide. UI kit, admin, tour, application root, and feature folders have
additional local rules.

## API SDK

The user API has an OpenAPI-generated Angular SDK under `src/app/shared/api/sdk/`.
The generated contract covers 41 groups and 262 operations. Forty-six user frontend
services use generated clients, including meals/recipes, dashboard/statistics,
cycles, the dietologist workspace, user profiles, notifications, auth/session,
AI, image coordination, exports, telemetry and marketing. Feature
adapters preserve UI models, date/quantity contracts and error behavior while
the SDK uses the existing HttpClient pipeline, per-call headers and contexts.
Cross-feature capability implementations load on demand at the composition root
and preserve request identity across retries. Swagger retains C# nullable reference
metadata in the isolated client schema; direct storage uploads and SignalR stay in their owning adapters.

The admin app has a separate generated client under
`projects/fooddiary-admin/src/app/shared/api/sdk/`. Its 21 groups and 72 operations
serve 21 existing admin adapters; generator tooling and schema selection are
shared with the user app, while application imports and session ownership stay
separate. Admin paging, dynamic filters, SSO cookie policy and import idempotency
remain in the existing services. List meal plans use the actual summary model;
the editor fetches full detail separately.

The Telegram bot has its own generated C# client for 14 FoodDiary API operations
under `FoodDiary.Telegram.Bot/Api/Generated/`. Existing adapters retain credentials,
bounded responses, lease/security-version checks, UTC/calendar contracts and
decimal nutrition totals. The isolated SDK export includes the three hidden web
telemetry/attribution routes while published Swagger remains unchanged.

`npm run check:api-client-usage` protects both Angular apps from handwritten HTTP
requests and runs through lint, CI and pre-push. TypeScript source guards retain
only reviewed method/verb/target exceptions for generated transport forwarding,
interceptors, signed uploads and static translations. The architecture suite's
`GeneratedApiClientUsageTests` applies the corresponding policy to the bot.

Read the concrete generated method and adapter needed for the task. Regenerate
with `npm run sdk:update` after API changes, and use `npm run sdk:check:api` to
check the live contract and generated output. Use the corresponding
`sdk:admin:update` and `sdk:admin:check:api` commands for admin, or
`sdk:bot:update` and `sdk:bot:check:api` for the bot. See
[`API_SDK.md`](../../docs/frontend/API_SDK.md) for scope, prerequisites, CI
checks and the separate requirement for mobile API compatibility.
