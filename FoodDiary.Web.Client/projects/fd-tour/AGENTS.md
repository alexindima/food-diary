# Tour Library Guidelines

## Scope

Rules for `FoodDiary.Web.Client/projects/fd-tour/`.
Also apply the workspace guide at `FoodDiary.Web.Client/AGENTS.md`.

## Purpose

- Provide a reusable guided-tour engine for the client app and admin app.
- Keep app-specific tour scenarios, translated copy, and product decisions in the consuming application.
- Keep this library focused on overlay rendering, target resolution, step navigation, persistence contracts, and public types.

## Commands

- Build library: `cd FoodDiary.Web.Client && npm run build:tour`
- Tests: `cd FoodDiary.Web.Client && npm run test:ci:tour`
- Lint: `cd FoodDiary.Web.Client && npm run lint`

## Standards

- Keep the public API explicit through `src/public-api.ts`.
- Do not depend on app feature code, admin feature code, or `fd-ui-kit`.
- Do not depend on translation libraries; consumers pass localized display strings.
- Keep browser-only behavior guarded for SSR.
- Prefer signal-based state and standalone Angular components/directives.
