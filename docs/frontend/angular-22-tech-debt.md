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

`@storybook/angular@10.5.5` supports Angular 22 (`>=18.0.0 <23.0.0`) and TypeScript 6 (`^4.9.0 || ^5.0.0 || ^6.0.0`). The workspace therefore installs without legacy peer resolution, and the temporary `legacy-peer-deps=true` setting has been removed.

TypeScript 7 cannot be adopted yet: Angular 22 and the current TypeScript ESLint packages do not support it.

`@angular-devkit/build-angular` is deprecated for application builds in Angular 22, but the current Storybook Angular preview builder still declares it as a required peer dependency and uses its webpack configuration helpers.

Without this dev dependency, `npm run build:storybook` fails with:

```text
Cannot find module '@angular-devkit/build-angular/package.json'
```

Keep `@angular-devkit/build-angular` as a dev-only Storybook compatibility dependency until Storybook no longer requires the legacy Angular webpack builder.

Close this when:

- `npm run build:storybook` works without `@angular-devkit/build-angular`.
- `@angular-devkit/build-angular` is removed from `devDependencies`.

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
