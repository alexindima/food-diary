# Angular 22 Tooling Compatibility

This document records the current tooling compatibility after the Angular 22 upgrade.

## Current State

- Application and admin builds use `@angular/build:*` builders in `FoodDiary.Web.Client/angular.json`.
- UI kit library build and unit tests use `@angular/build:*` builders.
- `ChangeDetectionStrategy.Eager` is not used in the codebase.
- ESLint blocks `changeDetection: ChangeDetectionStrategy.Eager`.
- `$safeNavigationMigration(...)` is not used in the codebase.
- Angular ESLint packages are on the Angular 22-compatible `22.x` line.
- Angular template diagnostics for `nullishCoalescingNotNullable` and `optionalChainNotNullable` are enforced as errors.
- Incremental hydration uses the Angular 22 default behavior and is covered by the client smoke suite.
- Storybook uses the Vite-based `@storybook/angular-vite` framework; the legacy Angular webpack builder is no longer required.

## Storybook

`@storybook/angular-vite@10.6.1` supports Angular 22 and TypeScript 6. Both Storybook Angular CLI targets use this framework with `.storybook/tsconfig.json`.

The framework is currently in preview, with stable support planned for Storybook 11. This workspace accepts that preview status. See the [official Angular Vite framework documentation](https://storybook.js.org/docs/get-started/frameworks/angular-vite).

- `@angular-devkit/build-angular`, `@storybook/angular`, and the Storybook webpack middleware override are removed.
- `@analogjs/vite-plugin-angular`, Vite, and Sass are explicit dev dependencies.
- Global styles and SCSS include paths are declared on both Storybook targets; Vite resolves TypeScript path aliases through `resolve.tsconfigPaths`.
- All stories and preview decorators import the Vite framework.
- The preview supplies the translation service and loads the existing English assets for components that require localization.

The workspace installs without legacy peer resolution. Storybook builds without the legacy Angular webpack builder, so the Storybook compatibility debt is closed.

## Remaining Compatibility Limit

TypeScript 7 cannot be adopted yet: Angular 22 and the current TypeScript ESLint packages do not support it.

## Verification Commands

Run these when changing the tooling:

```powershell
cd FoodDiary.Web.Client
npm run lint
npm run build
npm run build:admin
npm run build:storybook
npm run test:ci:ui-kit
npx tsc --project .storybook/tsconfig.json --noEmit
```

Also start `npm run storybook` and check that stories render with their styles and translations, controls update the preview, and keyboard interactions work without runtime errors.
