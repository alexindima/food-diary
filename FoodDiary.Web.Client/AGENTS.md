# Frontend (Angular) Guidelines

## Scope
Rules for the SPA workspace in `FoodDiary.Web.Client/`.
For UI kit specific work, also apply: `projects/fd-ui-kit/AGENTS.md`.

## Structure
- App sources: `src/`
- Shared library: `projects/fd-ui-kit/`
- Admin app: `projects/fooddiary-admin/`

## Commands
- Install: `npm install`
- Build: `npm run build`
- Lint: `npm run lint`
- Stylelint: `npm run stylelint`
- Prettier: `npm run prettier`
- Tests: `npm run test`
- UI kit build: `npx ng build fd-ui-kit`

## TypeScript + Angular
- Strict typing, avoid `any` (use `unknown` when needed).
- Use standalone components defaults (do not set `standalone: true` manually).
- Prefer signals and `computed()`, use `set`/`update`, avoid `mutate`.
- Prefer `input()` / `output()` helpers instead of decorators.
- Use `inject()` instead of constructor injection where practical.
- Use native template control flow: `@if`, `@for`, `@switch`.
- Prefer class/style bindings over `ngClass`/`ngStyle`.

## SCSS
- Import shared variables as:
  - `@use 'variables' as variables;`
- Reference tokens with explicit namespace:
  - `variables.$gap-m`
- Do not use:
  - `@use 'variables' as *;`

## UI/UX
- Keep mobile header/card patterns consistent across list pages.
- Respect existing design system components from `fd-ui-kit`.
- Meet WCAG AA and keep keyboard/focus behavior explicit.

## Localization
- Update both locale files for UI copy changes:
  - `assets/i18n/en.json`
  - `assets/i18n/ru.json`
- Verify Cyrillic output after edits.
