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
