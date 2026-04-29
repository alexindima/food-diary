# Frontend Style Governance

This guide defines how frontend styles should be added and reviewed. The goal is to keep component styles small, predictable, and centrally controlled through the design system.

## Core Rule

Use the most shared styling layer that fits the problem.

1. Use an existing `fd-ui-kit` component when the UI behavior already exists.
2. Use global utility classes for common layout, text, surface, effect, and state patterns.
3. Use CSS design tokens for values that represent spacing, sizing, typography, radius, color, borders, shadows, or effects.
4. Add component SCSS only for local structure, component-specific layout, and one-off geometry that should not become a system rule.

## Design Tokens

Design tokens live in `src/styles/design-tokens.scss` and are exposed as CSS custom properties.

Use tokens for:

- spacing: `var(--fd-space-md)`, `var(--fd-space-card-content-gap)`;
- sizes: `var(--fd-size-control-compact)`, `var(--fd-size-icon-lg)`;
- typography: `var(--fd-text-body-sm-size)`, `var(--fd-text-section-title-weight)`;
- radii: `var(--fd-radius-md)`, `var(--fd-radius-pill)`;
- colors and backgrounds: `var(--fd-color-text-muted)`, `var(--fd-bg-surface-raised)`;
- borders: `var(--fd-border-muted)`, `var(--fd-border-width-strong)`;
- shadows and effects: `var(--fd-shadow-lg)`, `var(--fd-transform-control-hover)`.

Do not add fallback values to design token reads:

```scss
/* Good */
gap: var(--fd-space-card-content-gap);

/* Avoid */
gap: var(--fd-space-card-content-gap, 16px);
```

Fallbacks hide missing token bugs. If a token is required, define it centrally.

## Utility Classes

Use utility classes when the same styling intent appears across components. Utilities are preferred for reusable composition and should keep component SCSS focused on component-specific structure.

Good utility candidates:

- repeated typography treatments;
- repeated surfaces and cards;
- common flex/grid alignment;
- common spacing stacks;
- common borders, shadows, and effects;
- state styling such as muted text, empty-state layout, or visually hidden text.

Avoid utility classes for:

- a layout that only exists inside one component;
- chart, SVG, canvas, image cropper, or hero artwork geometry;
- values that depend on data visualization math;
- styles that are clearer as a UI kit component API.

## Component SCSS

Component SCSS may define:

- host display and local grid/flex structure;
- responsive behavior with shared media aliases;
- local CSS variables that describe component-only geometry;
- selectors needed for component internals.

Component SCSS should not define:

- raw colors when an `--fd-color-*` or `--fd-bg-*` token exists;
- repeated pixel spacing, gaps, or padding;
- repeated control/icon sizes;
- repeated radii, shadows, borders, or transforms;
- `var(--fd-..., fallback)` token reads.

## Variables SCSS

`src/styles/variables.scss` is for Sass-only compile-time helpers, especially media query aliases.

Use it like this:

```scss
@use 'variables' as variables;

@media #{variables.$media-tablet} {
    /* responsive rules */
}
```

Do not use `variables.scss` as a runtime token source. Runtime styling should use CSS tokens.

## When To Add A New Token

Add a token when a value is:

- repeated in multiple components;
- part of a reusable UI primitive;
- affected by classic/modern density, theme, typography, or layout decisions;
- needed to keep component overrides consistent.

Do not add a token when a value is:

- local chart or SVG geometry;
- a one-off hero/landing illustration dimension;
- a container width that only belongs to one page;
- a data visualization stroke, radius, or coordinate;
- a browser reset value such as `0`, `100%`, `auto`, `1fr`, or `1em`.

## Review Checklist

Before merging frontend style changes:

- No raw hex/rgb colors outside token definitions.
- No hardcoded non-zero `margin`, `padding`, or `gap` when a spacing token fits.
- No fallback values in `var(--fd-...)` reads.
- Repeated sizes use `--fd-size-*` tokens.
- Repeated radii use `--fd-radius-*` tokens.
- Repeated shadows/effects use `--fd-shadow-*` or effect tokens.
- Component SCSS stays local and does not recreate UI kit primitives.
- Storybook docs/examples are updated when a shared pattern or token group changes.

## Useful Audit Commands

```bash
rg -n -g "*.scss" -g "*.css" "#[0-9a-fA-F]{3,8}|rgba?\(" FoodDiary.Web.Client
rg -n --pcre2 -g "*.scss" -g "*.css" "var\(--fd-[\w-]+\s*," FoodDiary.Web.Client
rg -n --pcre2 -g "*.scss" -g "*.css" "(?:margin|padding|gap):\s*(?:-?[1-9]|0?\.\d)" FoodDiary.Web.Client
```

Filter out `design-tokens.scss`, `variables.scss`, `dist`, and `node_modules` when reviewing results.
