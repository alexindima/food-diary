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

## Page Card Spacing

The page container owns the gap between the page header and the first content:
use `--fd-page-header-gap`, which follows the selected UI density. Do not add
header margins or content top padding to repeat it. Keep section/card spacing
inside the page body, and hide empty leading wrappers so they do not consume a
flex/grid gap. Custom page headers follow the same token.

Use `--fd-space-page-card-gap` for both horizontal and vertical gaps between peer cards (dashboard and body metric pages). It follows the selected UI density: 16–24px in classic and 12–16px in modern. Dashboard gap tokens alias this shared value. Keep `--fd-space-card-content-gap` for content inside cards and `--fd-space-page-section-gap` for separate page sections.

## Responsive Control And Header Sizes

Use the same tokens and component contracts across palettes and both UI styles.

| Role                                        | Fine pointer        | Coarse pointer      |
| ------------------------------------------- | ------------------- | ------------------- |
| Standard page, form or dialog action (`md`) | 40px minimum height | 44px minimum target |
| Toolbar icon (`appearance="toolbar"`)       | 38px square         | 44px square         |
| Small secondary action (`sm`)               | 32px minimum height | 44px minimum target |
| Explicit prominent action (`lg`)            | 48px minimum height | 48px minimum target |

Keep `lg` for prominent authentication and public landing actions. Fasting controls and unsaved-change dialog actions use the standard size. Equivalent toolbar buttons must not acquire page-local widths. Explicit dense secondary rows retain their documented compact exception; image surfaces and data-visualization geometry are separate contracts.

At widths up to 768px, page-title utilities use `--fd-text-page-title-mobile-size` (1.375rem, 22px at the default root font size). `fd-page-header` uses 4px vertical padding and a 44px minimum height that can grow for wrapped titles/subtitles. Keep the title and subtitle together with `--fd-space-micro`; center back and action buttons beside the complete text block without negative vertical margins. Action targets stay 44px on touch screens. Custom page headings must use the same title utilities.

Use `--fd-space-card-content-gap` between a widget header and its body: 12–16px in Classic and 12px in Modern. The dashboard widget frame and ordinary cards share this spacing contract on mobile and desktop. A title and its immediately related explanatory sentence may use `--fd-space-xxs` (4px), as in the diary setup hint; that is a different relationship from a header followed by data or controls.
