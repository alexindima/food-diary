# Angular 22 Migration Tech Debt

This document tracks temporary follow-up work after the Angular 22 upgrade.

## Current State

- Application and admin builds use `@angular/build:*` builders in `FoodDiary.Web.Client/angular.json`.
- UI kit library build and unit tests use `@angular/build:*` builders.
- `ChangeDetectionStrategy.Eager` is not used in the codebase.
- ESLint blocks `changeDetection: ChangeDetectionStrategy.Eager`.
- `$safeNavigationMigration(...)` is not used in the codebase.
- Angular ESLint packages are on the Angular 22-compatible `22.x` line.
- Angular template diagnostics for `nullishCoalescingNotNullable` and `optionalChainNotNullable` are enforced as errors.
- Incremental hydration uses the Angular 22 default behavior and is covered by the client smoke suite.

## Temporary Debt

### Storybook still needs `@angular-devkit/build-angular`

`@angular-devkit/build-angular` is deprecated for application builds in Angular 22, but the current `@storybook/angular@10.5.0` preview builder still imports its webpack configuration helpers and declares an Angular peer range below 22.

Without this dev dependency, `npm run build:storybook` fails with:

```text
Cannot find module '@angular-devkit/build-angular/package.json'
```

Keep `@angular-devkit/build-angular` as a dev-only Storybook compatibility dependency until Storybook publishes Angular 22-compatible support that no longer requires it.

`FoodDiary.Web.Client/.npmrc` also temporarily sets `legacy-peer-deps=true` so GitHub Actions `npm ci` and the frontend Docker build can install dependencies with the current Storybook Angular peer range.

Close this when:

- `@storybook/angular` supports Angular 22 peer ranges.
- `npm run build:storybook` works without `@angular-devkit/build-angular`.
- `@angular-devkit/build-angular` is removed from `devDependencies`.
- `FoodDiary.Web.Client/.npmrc` is removed, or at least no longer needs `legacy-peer-deps=true`.

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
