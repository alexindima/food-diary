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
- App tests: `npm run test:ci:app`
- UI kit tests: `npm run test:ci:ui-kit`
- Admin tests: `npm run test:ci:admin`
- UI kit build: `npx ng build fd-ui-kit`

## TypeScript + Angular

- Strict typing, avoid `any` (use `unknown` when needed).
- Use standalone components defaults (do not set `standalone: true` manually).
- Prefer signals and `computed()`, use `set`/`update`, avoid `mutate`.
- Prefer `input()` / `output()` helpers instead of decorators.
- Use `inject()` instead of constructor injection where practical.
- Avoid legacy Angular lifecycle hooks in app code.
- Prefer `constructor`, `effect()`, `computed()`, `takeUntilDestroyed()`, signal `viewChild()/contentChild()`, and `afterNextRender()` for initialization and post-render work.
- Do not introduce `OnInit`, `OnChanges`, `OnDestroy`, `AfterViewInit`, `AfterViewChecked`, `AfterContentInit`, `AfterContentChecked`, or `DoCheck` in new code.
- Use native template control flow: `@if`, `@for`, `@switch`.
- Prefer class/style bindings over `ngClass`/`ngStyle`.

## SCSS

- Import shared variables as:
    - `@use 'variables' as variables;`
- Use `variables` only for Sass-only media query aliases:
    - `@media #{variables.$media-tablet}`
- Use CSS design tokens for runtime styling:
    - `var(--fd-space-md)`
    - `var(--fd-radius-md)`
    - `var(--fd-text-body-size)`
- Follow `STYLE_GUIDE.md` before adding or changing component styles.
- Prefer existing `fd-ui-kit` components and global utility classes before adding new component-local SCSS.
- Use tokens for spacing, sizing, typography, radii, colors, backgrounds, borders, shadows, and effects whenever a matching token exists.
- Do not add fallback values to `var(--fd-...)` token reads; missing tokens should be fixed centrally.
- Do not hardcode non-zero `margin`, `padding`, or `gap` when a spacing token fits.
- Keep local hardcoded geometry only for one-off chart/SVG/canvas/hero/container math where a global token would reduce clarity.
- Do not use:
    - `@use 'variables' as *;`

## UI/UX

- Keep mobile header/card patterns consistent across list pages.
- Respect existing design system components from `fd-ui-kit`.
- Meet WCAG AA and keep keyboard/focus behavior explicit.

## Localization

- Update both locale files for UI copy changes:
    - `assets/i18n/en/*.json`
    - `assets/i18n/ru/*.json`
- Verify Cyrillic output after edits.
