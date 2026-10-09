# Admin Frontend Guidelines

## Scope

Rules for `FoodDiary.Web.Client/projects/fooddiary-admin/`.
Also apply the workspace guide at `FoodDiary.Web.Client/AGENTS.md`.

## Role

- Angular admin application for operational and privileged workflows.
- Reuse `fd-ui-kit` primitives and workspace shared patterns instead of adding admin-only one-off UI primitives.

## Structure

- Feature code belongs under `src/app/features/<feature>/`.
- Feature folders should use concrete layers such as `api`, `models`, `components`, `dialogs`, `pages`, and `*.routes.ts`.
- Route configuration should stay in `app.routes.ts` or feature `*.routes.ts` files.

## Rules

- Follow Angular standalone, signals, `input()`/`output()`, `inject()`, and `OnPush` conventions from the workspace guide.
- Do not deep-import from `projects/fd-ui-kit/src/lib/**`; import from the public UI kit surface.
- Do not import Angular Material/CDK overlay primitives directly in feature code when a UI kit primitive exists.
- Keep guards in the routing layer.
- Avoid legacy global `pages/`, `services/`, and `guards/` buckets for new feature code.
- Update both `assets/i18n/en/*.json` and `assets/i18n/ru/*.json` when admin UI copy changes.

## Commands

- Build: `cd FoodDiary.Web.Client && npm run build:admin`
- Test: `cd FoodDiary.Web.Client && npm run test:ci:admin`
- Lint: `cd FoodDiary.Web.Client && npm run lint`
- API client update: `cd FoodDiary.Web.Client && npm run sdk:admin:update`
- API client drift check: `cd FoodDiary.Web.Client && npm run sdk:admin:check:api`
- Do not hand-edit `src/app/shared/api/sdk/generated/`. Use concrete generated services and DTOs through the existing admin feature adapters, preserving SSO ownership, cookie policy, filters and idempotency.
- The admin contract and frozen operation manifest live in `FoodDiary.Web.Client/api-sdk/admin.openapi.json` and `admin.scopes.json`; generation tooling is shared with the user client. Admin runtime code must retain its existing boundary from the main app sources.

Admin owns its own AdminId/CalendarDate/UtcInstant namespace under shared/models/semantics; tag raw SDK/route/form/placeholder values explicitly without stricter formatting. Catalog transfer DTOs remain scalar wire/file structures. DecodeCatalogIngredientSource binds product amounts versus recipe servings for actual reference validation/ordering, preserving text-only and legacy dual-reference rows. Keep external provider IDs and domain text as their established scalars.
