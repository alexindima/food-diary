# Angular App Guidelines

## Scope
Rules for `FoodDiary.Web.Client/src/app/`.
Also apply `FoodDiary.Web.Client/AGENTS.md`.

## Structure
- Feature code belongs in `features/<feature>/`.
- Shared API clients and API-facing helpers belong in `shared/api/`.
- Shared models belong in `shared/models/`.
- Feature-agnostic shared UI belongs in `components/shared/`.
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
- Do not use direct browser globals in runtime code; preserve SSR compatibility.

## Verification
- Lint: `cd FoodDiary.Web.Client && npm run lint`
- Dependency graph: `cd FoodDiary.Web.Client && npm run lint:deps:strict`
- App tests: `cd FoodDiary.Web.Client && npm run test:ci:app`
