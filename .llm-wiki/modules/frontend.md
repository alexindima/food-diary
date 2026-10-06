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

Products has an OpenAPI-generated Angular SDK under `src/app/shared/api/sdk/`.
The generated client covers ten Products operations; the initial ProductService
integration uses it for suggestions and deletion. Feature adapters preserve UI
models and error behavior while the SDK uses the existing HttpClient pipeline.

Read the concrete generated method and adapter needed for the task. Regenerate
with `npm run sdk:update` after API changes, and use `npm run sdk:check:api` to
check the live contract and generated output. See
[`API_SDK.md`](../../docs/frontend/API_SDK.md) for scope, prerequisites, CI
checks and the separate requirement for mobile API compatibility.
