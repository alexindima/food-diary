# FdUiKit

Shared design system library for Food Diary.

## Style Governance

UI kit changes should keep reusable visual behavior centralized.

- Prefer component APIs, design tokens, and utility classes over consumer-level one-off overrides.
- Use CSS design tokens for spacing, sizing, typography, radii, colors, backgrounds, borders, shadows, and effects.
- Do not add fallback values to `var(--fd-...)` token reads.
- Do not hardcode repeated control sizes, icon sizes, spacing, radii, borders, shadows, or transforms.
- Keep local hardcoded values only for component-internal geometry that does not represent a reusable system decision.
- Update Storybook docs when adding shared token groups, utility patterns, or visual primitives.

The full frontend style guide is in [`../../STYLE_GUIDE.md`](../../STYLE_GUIDE.md).

## Storybook

Use Storybook as the visible design-system reference:

```bash
npm run storybook
```

Build Storybook before merging documentation or shared component changes when practical:

```bash
npm run build:storybook
```

## Code scaffolding

Angular CLI includes powerful code scaffolding tools. To generate a new component, run:

```bash
ng generate component component-name
```

For a complete list of available schematics (such as `components`, `directives`, or `pipes`), run:

```bash
ng generate --help
```

## Building

To build the library, run:

```bash
ng build fd-ui-kit
```

This command will compile your project, and the build artifacts will be placed in the `dist/` directory.

### Publishing the Library

Once the project is built, you can publish your library by following these steps:

1. Navigate to the `dist` directory:

    ```bash
    cd dist/fd-ui-kit
    ```

2. Run the `npm publish` command to publish your library to the npm registry:
    ```bash
    npm publish
    ```

## Running unit tests

To execute unit tests with the [Karma](https://karma-runner.github.io) test runner, use the following command:

```bash
ng test
```

## Running end-to-end tests

For end-to-end (e2e) testing, run:

```bash
ng e2e
```

Angular CLI does not come with an end-to-end testing framework by default. You can choose one that suits your needs.

## Additional Resources

For more information on using the Angular CLI, including detailed command references, visit the [Angular CLI Overview and Command Reference](https://angular.dev/tools/cli) page.
