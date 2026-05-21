# Frontend Architecture

## Workspace Shape
The Angular workspace lives in `FoodDiary.Web.Client`.

Major parts:
- `src/app` - primary web application.
- `projects/fooddiary-admin` - admin Angular application.
- `projects/fd-ui-kit` - reusable UI primitives, visual tokens, Storybook documentation.

## Main App Structure

Important folders in `src/app`:
- `features/` - feature-owned pages/components/API/model/lib code.
- `shared/api/` - shared API clients and API-facing helpers.
- `shared/models/` - pure shared models.
- `components/shared/` - feature-agnostic shared UI.
- `shell/`, `theme/`, `config/`, `interceptor/` - app-level infrastructure.
- `guards/` - route-layer guards.
- `testing/` - frontend test helpers.

Feature folders should prefer concrete layers:
- `api/`
- `models/`
- `components/`
- `dialogs/`
- `lib/`
- `pages/`
- `resolvers/`
- `<feature>.routes.ts`

## Admin App
The admin app follows the same feature-first approach under:

```text
projects/fooddiary-admin/src/app/features/
```

Admin feature code should not use legacy global `pages/`, `services/`, or `guards/` buckets for new functionality. Route guards belong in route configuration.

## UI Kit
Use `fd-ui-kit` for shared UI primitives and tokenized styling.

Rules:
- Import from the public UI kit surface.
- Do not deep-import `projects/fd-ui-kit/src/lib/**`.
- Prefer adding/fixing shared UI behavior in the UI kit over repeated app-level overrides.
- Update Storybook docs/examples when UI kit APIs, token groups, or visual primitives change.

## Angular Conventions
Follow the official Angular conventions reflected in ESLint and `AGENTS.md`:
- standalone components by default; do not set `standalone: true`,
- `ChangeDetectionStrategy.OnPush`,
- signals for local state,
- `computed()` for derived state,
- `input()` / `output()` helper APIs,
- `inject()` over constructor injection where practical,
- `host` metadata instead of `@HostBinding` / `@HostListener`,
- native template control flow (`@if`, `@for`, `@switch`),
- class/style bindings over `ngClass`/`ngStyle`,
- Reactive Forms for new form work,
- lazy-loaded feature routes.

## Dependency Boundaries
ESLint and Dependency Cruiser enforce frontend boundaries.

Key rules:
- Feature implementation code must not import route files.
- Feature roots should not be imported directly; import a concrete layer.
- Shared models must not depend on API, UI, or feature code.
- Shared API must not depend on UI or feature code.
- Shared UI must stay feature-agnostic.
- Feature components/dialogs/lib/resolvers should use same-feature or shared APIs instead of reaching into another feature API.
- Guards belong in app routes or feature route files.
- Avoid direct browser globals; preserve SSR compatibility.

## Styling
Styling should use design tokens and existing UI primitives.

Rules:
- Use `var(--fd-...)` runtime design tokens where available.
- Do not add fallback values to `var(--fd-...)` token reads.
- Use `@use 'variables' as variables;` only for Sass-only helpers such as media query aliases.
- Do not use `@use 'variables' as *;`.
- Prefer tokens/utilities/UI kit APIs over hardcoded component-local styling.

## Accessibility
The template lint config enforces accessibility rules.

Practical rules:
- Icon-only `fd-ui-button` controls need an `ariaLabel` input or visible text.
- Native controls should have explicit `for`/`id` label association.
- Keep keyboard and focus behavior explicit.
- Avoid positive tabindex and invalid ARIA.

## Localization
For UI text changes update both:
- `assets/i18n/en/*.json`
- `assets/i18n/ru/*.json`

Run:

```bash
npm run check:i18n
```

Check Cyrillic output for mojibake or replacement characters.
