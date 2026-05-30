# Angular App Guidelines

## Scope
Rules for `FoodDiary.Web.Client/src/app/`.
Also apply `FoodDiary.Web.Client/AGENTS.md`.

## Structure
- Feature code belongs in `features/<feature>/`.
- Shared API clients and API-facing helpers belong in `shared/api/`.
- Shared models belong in `shared/models/`.
- Shared cross-feature primitives should use explicit common-theme folders under `shared/` such as `forms/`, `i18n/`, `notifications/`, `platform/`, `theme/`, and `ui/`.
- Feature-agnostic shared UI belongs in `components/shared/`.
- Do not add new files to legacy top-level type buckets such as `services/`, `guards/`, `directives/`, `pipes/`, or `validators/`; move feature-specific code into its feature and common code into an explicit `shared/` theme.
- Route guards belong to route configuration usage, not arbitrary feature implementation imports.

## Angular Rules
- Use standalone components without explicitly setting `standalone: true`.
- Use `ChangeDetectionStrategy.OnPush`.
- Prefer signals, `computed()`, `input()`, `output()`, and `inject()`.
- Use `host` metadata instead of `@HostBinding` / `@HostListener`.
- Avoid new legacy lifecycle-hook usage; prefer signals, `effect()`, `afterNextRender()`, and `takeUntilDestroyed()`.
- Use native control flow (`@if`, `@for`, `@switch`) in templates.
- Prefer class/style bindings over `ngClass`/`ngStyle`.

## Import Boundaries
- Do not import feature roots directly. Import concrete feature layers.
- Do not import `*.routes.ts` from feature implementation code.
- Do not deep-import UI kit internals from `projects/fd-ui-kit/src/lib/**`.
- Do not import Angular Material/CDK overlay/layout directly in app feature code when a UI kit or shared service abstraction exists.
- Do not inject or import `HttpClient` directly from components, dialogs, pages, or shared UI; keep HTTP transport in API services or a feature lib/facade.
- Do not add new direct API-service imports from components, dialogs, pages, or shared UI; route behavior through feature `lib/`/facades unless an existing ESLint baseline entry is being migrated away.
- Do not use direct browser globals in runtime code; preserve SSR compatibility.

## Verification
- Lint: `cd FoodDiary.Web.Client && npm run lint`
- Dependency graph: `cd FoodDiary.Web.Client && npm run lint:deps:strict`
- App tests: `cd FoodDiary.Web.Client && npm run test:ci:app`
