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
- Production build: `npm run build:prod`
- Admin build: `npm run build:admin`
- Lint: `npm run lint`
- Dependency graph lint: `npm run lint:deps:strict`
- Stylelint: `npm run stylelint`
- Prettier: `npm run prettier`
- Tests: `npm run test`
- App tests: `npm run test:ci:app`
- UI kit tests: `npm run test:ci:ui-kit`
- Admin tests: `npm run test:ci:admin`
- Full frontend verification: `npm run verify`
- i18n check: `npm run check:i18n`
- SEO prerender check: `npm run check:seo-prerender`
- Client smoke E2E: `npm run test:e2e:client:smoke`
- Admin smoke E2E: `npm run test:e2e:admin:smoke`
- Storybook: `npm run storybook`
- Storybook build: `npm run build:storybook`
- UI kit build: `npx ng build fd-ui-kit`

## TypeScript + Angular

- Strict typing, avoid `any` (use `unknown` when needed).
- Do not cast to `any`.
- Use standalone components defaults (do not set `standalone: true` manually).
- Set `changeDetection: ChangeDetectionStrategy.OnPush` on components.
- Prefer signals and `computed()`, use `set`/`update`, avoid `mutate`.
- Prefer `input()` / `output()` helpers instead of decorators.
- Use `inject()` instead of constructor injection where practical.
- Use `@Service()` for root singleton services. Keep `@Injectable()` for constructor DI, advanced provider configuration, and non-root scopes.
- Use `host` metadata instead of `@HostBinding` / `@HostListener`.
- Avoid legacy Angular lifecycle hooks in app code.
- Prefer `constructor`, `effect()`, `computed()`, `takeUntilDestroyed()`, signal `viewChild()/contentChild()`, and `afterNextRender()` for initialization and post-render work.
- Do not introduce `OnInit`, `OnChanges`, `OnDestroy`, `AfterViewInit`, `AfterViewChecked`, `AfterContentInit`, `AfterContentChecked`, or `DoCheck` in new code.
- Use native template control flow: `@if`, `@for`, `@switch`.
- Prefer class/style bindings over `ngClass`/`ngStyle`.
- Use lazy loading for feature routes.
- Use `NgOptimizedImage` for static images where applicable.
- Use Reactive Forms rather than template-driven forms for new form work.
- Prefer Signal Forms for new simple forms and migration pilots. Keep Reactive Forms for complex `FormArray`, dynamic validator, manual error, disabled-state, and CVA-heavy flows until local patterns are proven.
- Do not import from Angular internals (`@angular/*/src`) or RxJS internals.
- Import RxJS operators from `rxjs`, not `rxjs/operators`.

## Frontend Boundaries

- App features live under `src/app/features/<feature>/` with concrete layers such as `api`, `models`, `components`, `dialogs`, `lib`, `pages`, `resolvers`, and `*.routes.ts`.
- Do not import route files from feature implementation code.
- Do not import feature roots directly; import a concrete layer.
- Shared models must not depend on API, UI, or feature-local code.
- Shared API code must not depend on UI or feature-local code.
- Shared UI under `src/app/components/shared` must remain feature-agnostic.
- Feature components/dialogs/lib/resolvers should not reach directly into another feature API unless the boundary is explicitly shared.
- Import UI primitives from `fd-ui-kit`; do not deep-link into `projects/fd-ui-kit/src/lib/**`.
- Avoid direct Angular Material/CDK overlay/layout imports in app/admin feature code when a UI kit or `ViewportService` abstraction exists.
- Guards belong to app route files or feature route files.
- Do not use browser globals (`window`, `document`, `navigator`, `localStorage`, `sessionStorage`) directly in runtime code. Inject `DOCUMENT`, use renderer abstractions, and guard browser-only work for SSR.

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
- Icon-only `fd-ui-button` controls need an `ariaLabel` input or visible projected text.
- Associate native form controls with explicit `for`/`id` labels; do not wrap controls in labels.
- Templates should stay simple. ESLint enforces low template conditional/cyclomatic complexity.

## Localization

- Update both locale files for UI copy changes:
    - `assets/i18n/en/*.json`
    - `assets/i18n/ru/*.json`
- Verify Cyrillic output after edits.
