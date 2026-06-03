# Angular 22 Migration Tech Debt

This document tracks temporary follow-up work after the Angular 22 upgrade.

## Current State

- Application and admin builds use `@angular/build:*` builders in `FoodDiary.Web.Client/angular.json`.
- UI kit library build and unit tests use `@angular/build:*` builders.
- `ChangeDetectionStrategy.Eager` is not used in the codebase.
- ESLint blocks `changeDetection: ChangeDetectionStrategy.Eager`.
- `$safeNavigationMigration(...)` is not used in the codebase.

## Temporary Debt

### Storybook still needs `@angular-devkit/build-angular`

`@angular-devkit/build-angular` is deprecated for application builds in Angular 22, but the current `@storybook/angular@10.4.2` preview builder still resolves `@angular-devkit/build-angular/package.json`.

Without this dev dependency, `npm run build:storybook` fails with:

```text
Cannot find module '@angular-devkit/build-angular/package.json'
```

Keep `@angular-devkit/build-angular` as a dev-only Storybook compatibility dependency until Storybook publishes Angular 22-compatible support that no longer requires it.

Close this when:

- `@storybook/angular` supports Angular 22 peer ranges.
- `npm run build:storybook` works without `@angular-devkit/build-angular`.
- `@angular-devkit/build-angular` is removed from `devDependencies`.

### Angular ESLint has no Angular 22 peer range yet

`angular-eslint@21.4.0` currently declares `@angular/cli >= 21.0.0 < 22.0.0`.

Keep the current version only as a temporary lint tooling bridge.

Close this when:

- Angular ESLint publishes an Angular 22-compatible version.
- `@angular-eslint/*` and `angular-eslint` are upgraded together.
- `npm run lint` still passes.

### Template diagnostics are suppressed for migration compatibility

The Angular 22 migration added suppressions for:

- `nullishCoalescingNotNullable`
- `optionalChainNotNullable`

These are currently present in frontend app, admin app, and UI kit tsconfigs. They should be treated as migration debt, not as permanent defaults.

Close this when:

- Templates are reviewed and unnecessary optional chains/nullish coalescing are removed.
- The suppressions are removed from tsconfigs.
- `npm run build`, `npm run build:admin`, and `npm run test:ci:ui-kit` pass.

## Verification Commands

Run these before closing the migration debt:

```powershell
cd FoodDiary.Web.Client
npm run lint
npm run build
npm run build:admin
npm run build:storybook
npm run test:ci:ui-kit
```
