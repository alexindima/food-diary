# FoodDiary.Web.Client

Angular 21 SPA for nutrition tracking with a shared UI library and admin app.

## Workspace

| Project | Type | Root | Port |
|---------|------|------|------|
| food-diary-web-client | application | src/ | 4200 |
| fd-ui-kit | library | projects/fd-ui-kit/ | — |
| fooddiary-admin | application | projects/fooddiary-admin/ | 4300 |

## Key Configuration

- **Angular 21.2.6**, zoneless (`provideZonelessChangeDetection()`)
- **Strict TypeScript**: `strict: true`, `strictTemplates`, `strictInjectionParameters`
- **SCSS** for styles
- **i18n**: `@ngx-translate/core` with JSON files at `assets/i18n/`, Russian locale registered at bootstrap
- **Service Worker**: enabled in non-dev mode
- **Path aliases**: `fd-ui-kit` -> `projects/fd-ui-kit/src/public-api`, `fd-ui-kit/*` -> `projects/fd-ui-kit/src/lib/*`

## Component Conventions

### Mandatory
- **Standalone components** — no NgModules, all components are standalone
- **OnPush change detection** — default for all components
- **Signals** — `signal()` for state, `computed()` for derived, `input()` / `output()` for component APIs, `viewChild()` for template refs
- **`inject()` function** — never constructor injection
- **Native control flow** — `@if`, `@for`, `@switch` (never `*ngIf`, `*ngFor`)
- **Import from fd-ui-kit** — `fd-ui-kit` or `fd-ui-kit/...` for shared UI components; never import `@angular/material` or deep-link into `projects/fd-ui-kit/src/lib/**`
- **CDK boundary** — app/admin code may use `@angular/cdk/layout` and `@angular/cdk/drag-drop` directly, but overlay/dialog/portal primitives must stay behind `fd-ui-kit`
- **Viewport boundary** — app feature code should use `ViewportService` for the shared mobile breakpoint instead of injecting `BreakpointObserver` directly

### State Management
No NgRx. State via Angular Signals in services and components. `AuthService` holds auth state as signals. Feature services extend `ApiService` base class.

### HTTP Layer
- `ApiService` base class (`services/api.service.ts`) — typed `get/post/put/patch/delete` wrappers
- `RetryInterceptor` — retries GET 3x with exponential backoff, skips 4xx
- `AuthInterceptor` — attaches Bearer token, 401 triggers refresh then retry

### Routing
- Authenticated routes lazy-loaded via `loadChildren()` with `PreloadAllModules`
- `authGuard` checks `isAuthenticated()` + `isEmailConfirmed()`
- SEO via `data.seo` on each route (titleKey, descriptionKey, noIndex)
- Feature routes use container/child pattern

### Cleanup Pattern
`takeUntilDestroyed(this.destroyRef)` for RxJS subscription cleanup.

## Feature Organization

```
src/app/features/{feature}/
  api/          — services extending ApiService
  models/       — data types
  pages/        — routed components
  components/   — presentational components
  dialogs/      — modal components
  lib/          — utilities
  resolvers/    — route resolvers
```

Shared components at `src/app/components/shared/`.

## fd-ui-kit Library

Design-system layer for shared primitives and theme tokens. Components: button, card, input, textarea, select, calendar/date inputs, checkbox, radio, dialog, toast, menu, tabs, pagination, loader, etc. Angular CDK remains an internal implementation detail where needed.

Has Storybook stories — `npm run storybook` on port 6006.

## Environment Configs (`src/environments/`)

Three environments (dev, staging, prod). `__BUILD_VERSION__` placeholder replaced at deploy time. `AppConfig` type at `src/app/types/app.data.ts`.

## Build Commands

```bash
npm install          # Install dependencies
npm run start        # Dev server at localhost:4200
npm run build:prod   # Production build
npm run lint:fix     # ESLint auto-fix
npm run stylelint:fix # Style linting
npm run prettier:fix # Format code
npm run test         # Karma/Jasmine tests
npx ng build fd-ui-kit # Build shared UI library
```
