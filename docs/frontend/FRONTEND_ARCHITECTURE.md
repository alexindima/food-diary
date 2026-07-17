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

## State Ownership and Lifetime

The frontend uses native Angular signals and signal-based facades. NgRx Store, NgRx SignalStore, and ComponentStore are intentionally not part of the architecture. Introduce another state framework only through an ADR that demonstrates a cross-feature problem the existing model cannot solve cleanly.

Every piece of mutable client state must have one explicit owner and the narrowest useful lifetime:

| State kind | Owner | DI/lifetime |
|---|---|---|
| Authentication, current-user capabilities, theme, notifications, global loading | Small app/shared state service | Root singleton via `@Service()` |
| Feature data, loading/error state, drafts, and feature use-case orchestration | Feature facade | Route, page, dialog, or owning component provider via `@Injectable()` |
| Form interaction, expanded panels, focus, hover, crop, and other transient presentation state | Component | Component instance |
| Filters, tabs, paging, or selection that must survive reload or be shareable | Angular Router | URL path/query parameters |
| Persisted browser preferences | A platform/storage service | Browser storage behind an SSR-safe abstraction |
| Authoritative business data | Backend API | Do not create an unrelated global client copy |

Rules:

- Root signal state is exceptional. A new root state service needs a cross-feature consumer and a clear reset policy for logout/account changes.
- Stateful feature facades must use `@Injectable()` and be listed in the owning route/page/dialog/component `providers`. They must not rely on a root fallback.
- Expose state as readonly signals when consumers do not own the mutation. Keep writable signals private unless template/form integration requires otherwise.
- Put derived state in `computed()` and state transitions/use cases in facade methods. Do not mirror the same mutable value in both a component and a facade.
- Keep server-state caching local to the owning feature unless multiple features require coordinated caching and invalidation.
- A facade may combine state, commands, and API orchestration. Reserve the `Store` suffix for a class whose primary responsibility is state transitions rather than use-case orchestration.
- Direct `localStorage`, `sessionStorage`, browser globals, and cross-feature API access remain prohibited at the component boundary.

### Server State and Invalidation

- Model non-trivial asynchronous reads with `RequestStateController<T, TError>` instead of independent `isLoading`, `error`, data, and request-version signals. The controller owns stale-response rejection and preserves existing data during refresh.
- Keep simple one-shot commands as tracked operations; do not force every button action into a long-lived request state.
- Choose cache behavior explicitly in the owning facade: network-only on entry, owner-lifetime cache, stale-while-revalidate, or session cache. Session caching requires an explicit logout/reset path.
- Report mutations through a narrow semantic invalidation service only when another mounted read model can become stale. Do not introduce a generic string-based event bus.
- Invalidation versions are refresh hints, not authoritative data. Consumers must reload from the owning API and remain correct if they mount after an event occurred.

Automated enforcement is intentionally limited to statically provable rules. `npm run check:state-ownership` verifies stateful facade scope and provider ownership; ESLint verifies their decorator choice. Architectural review remains responsible for semantic decisions such as whether a value truly needs global lifetime.

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
