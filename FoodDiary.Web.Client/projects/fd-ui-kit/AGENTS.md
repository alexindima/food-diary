# UI Kit Guidelines

## Scope

Rules for `FoodDiary.Web.Client/projects/fd-ui-kit/`.

## Purpose

- Centralize reusable UI primitives and visual tokens.
- Prefer fixing shared behavior here over page-level one-off overrides.

## Commands

- Build library: `npx ng build fd-ui-kit`
- Tests: `cd FoodDiary.Web.Client && npm run test:ci:ui-kit`
- Storybook: `cd FoodDiary.Web.Client && npm run storybook`
- Storybook build: `cd FoodDiary.Web.Client && npm run build:storybook`
- Reference docs: `COMPONENTS.md`

## Standards

- Keep public API stable and explicit.
- Follow Angular strict typing conventions.
- Use standalone components without explicitly setting `standalone: true`.
- Use `ChangeDetectionStrategy.OnPush`.
- Prefer `input()` / `output()` helpers.
- Prefer `inject()` over constructor injection where practical.
- Use `host` metadata instead of `@HostBinding` / `@HostListener`.
- Keep styles token-driven; avoid hardcoded colors when design token exists.
- Use CSS design tokens for runtime styling, for example `var(--fd-space-md)` and `var(--fd-radius-md)`.
- Use `@use 'variables' as variables;` only when a Sass-only helper such as a media query alias is needed.
- Prefer token, utility, or component API changes over consumer-level one-off overrides.
- Use shared tokens for repeated control sizes, icon sizes, spacing, radii, borders, shadows, and transforms.
- Do not add fallback values to `var(--fd-...)` token reads.
- Update Storybook documentation when adding token groups, utility patterns, or shared visual primitives.
- Keep public exports intentional. Consumers should import from the UI kit public surface, not deep paths.
- Preserve accessibility defaults. Icon-only controls must expose accessible naming APIs.
- Include `src/lib/icon/material-icons-subset.scss` alongside the full Material Icons stylesheet in consuming apps and Storybook. Known names use the checked-in small font; unknown names and explicit `fontSet` values preserve the full/custom font behavior. Regenerate the subset with `python scripts/generate-icon-subset.py` from the Angular workspace after installing `fonttools[woff]==4.63.0`; Python is not needed by ordinary frontend builds. Commit the generated font and name list together, preserving the adjacent Apache license.

## Change Policy

- If same UI behavior is duplicated across app pages, move it into UI kit.
- Keep component docs/examples updated when behavior or API changes.
