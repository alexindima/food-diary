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

### Storybook still needs legacy peer resolution and `@angular-devkit/build-angular`

`@storybook/angular@10.5.2` supports Angular 22 (`>=18.0.0 <23.0.0`), so the Angular peer range is no longer the blocker. However, its TypeScript peer range is still `^4.9.0 || ^5.0.0`, while Angular 22 requires TypeScript `>=6.0 <6.1`.

Without legacy peer resolution, `npm ci` fails with `ERESOLVE` because the workspace uses TypeScript 6. TypeScript 7 cannot be adopted yet either: Angular 22 and the current TypeScript ESLint packages do not support it.

`@angular-devkit/build-angular` is deprecated for application builds in Angular 22, but the current Storybook Angular preview builder still declares it as a required peer dependency and uses its webpack configuration helpers.

Without this dev dependency, `npm run build:storybook` fails with:

```text
Cannot find module '@angular-devkit/build-angular/package.json'
```

Keep `@angular-devkit/build-angular` as a dev-only Storybook compatibility dependency until Storybook no longer requires the legacy Angular webpack builder.

`FoodDiary.Web.Client/.npmrc` temporarily sets `legacy-peer-deps=true` so GitHub Actions `npm ci` and the frontend Docker build can install the Angular 22/TypeScript 6 workspace despite Storybook's stale TypeScript peer range.

Close this when:

- `@storybook/angular` supports the TypeScript version required by the current Angular version.
- The current TypeScript ESLint packages support that TypeScript version.
- `npm run build:storybook` works without `@angular-devkit/build-angular`.
- `@angular-devkit/build-angular` is removed from `devDependencies`.
- `npm ci` succeeds after `FoodDiary.Web.Client/.npmrc` is removed, or after `legacy-peer-deps=true` is removed from it.

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
