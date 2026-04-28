# UI Kit Guidelines

## Scope
Rules for `FoodDiary.Web.Client/projects/fd-ui-kit/`.

## Purpose
- Centralize reusable UI primitives and visual tokens.
- Prefer fixing shared behavior here over page-level one-off overrides.

## Commands
- Build library: `npx ng build fd-ui-kit`
- Reference docs: `COMPONENTS.md`

## Standards
- Keep public API stable and explicit.
- Follow Angular strict typing conventions.
- Prefer `input()` / `output()` helpers.
- Keep styles token-driven; avoid hardcoded colors when design token exists.
- Use CSS design tokens for runtime styling, for example `var(--fd-space-md)` and `var(--fd-radius-md)`.
- Use `@use 'variables' as variables;` only when a Sass-only helper such as a media query alias is needed.

## Change Policy
- If same UI behavior is duplicated across app pages, move it into UI kit.
- Keep component docs/examples updated when behavior or API changes.
