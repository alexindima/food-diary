# WebMCP Library Guidelines

## Scope

Rules for `FoodDiary.Web.Client/projects/fd-webmcp/`.
Also apply the workspace guide at `FoodDiary.Web.Client/AGENTS.md`.

## Purpose

- Share the browser WebMCP bridge, tool lifecycle/execution and screen contracts between the client and admin.
- Keep authentication, route allowlists, API clients, data redaction, tool schemas and domain scenarios in the consuming application.
- Export supported capabilities only through `src/public-api.ts`; use relative imports inside the library.
- Do not depend on either application, `fd-ui-kit`, `fd-tour`, translation libraries or generated API clients.
- Register tools only in local development after browser rendering. The consuming application owns session checks.
- Preserve explicit unsaved-change and revision protections. The library must not save records or request permissions.

## Commands

- Build: `cd FoodDiary.Web.Client && npm run build:webmcp`
- Tests: `cd FoodDiary.Web.Client && npm run test:ci:webmcp`
- Boundaries: `cd FoodDiary.Web.Client && npm run check:import-boundaries && npm run lint:deps:strict`
