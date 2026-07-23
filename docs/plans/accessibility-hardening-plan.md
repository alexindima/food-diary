# Accessibility hardening plan

## Audit scope

- Client: 23 public routes and 23 authenticated routes.
- Admin: 12 routes.
- Viewports: 1440 x 900 desktop and 390 x 844 mobile.
- States: authenticated and anonymous shells, mobile navigation, product filters, dashboard appearance dialog, list and create pages.
- Checks: axe WCAG A/AA rules, accessibility tree inspection, keyboard navigation, visible focus, interactive semantics, labels, contrast, target size, and scroll regions.

The initial automated pass covered 118 route/state/viewport combinations. It found 125 rule occurrences affecting 421 DOM nodes. Most occurrences came from a small number of shared components rather than page-specific defects.

## Remediation order

1. **Critical semantics and names**
   - Give every mobile navigation item an explicit accessible name when its visual label is hidden by responsive CSS.
   - Label search, calorie, macro, weight, and waist inputs.
   - Expose the date picker trigger as a combobox instead of placing unsupported popup state on a plain textbox.
2. **Interactive structure**
   - Replace nested interactive entity cards with one dedicated card-opening button plus independent preview, favorite, and action buttons.
   - Represent shopping-list cards as list items; keep selection and menu controls as sibling buttons.
3. **Keyboard operability**
   - Keep every action reachable with Tab and activatable with Enter/Space.
   - Make the independently scrolling application viewport focusable and named.
   - Retain the skip link and verify focus restoration for dialogs and sheets.
4. **Perceivability**
   - Bring muted text, nutrient values, labels, hints, and status text to WCAG AA contrast in both themes.
   - Keep favorite and other compact controls at least 24 x 24 CSS pixels, with 44 x 44 preferred for primary touch actions.
5. **Regression prevention**
   - Keep Angular template accessibility ESLint rules enabled.
   - Add component tests for responsive accessible names, popup roles, labels, and card semantics.
   - Run Playwright + axe over every authenticated route at desktop and mobile sizes with deterministic API mocks.
   - Keep Lighthouse checks for public pages and extend route coverage when new public pages are added.

## Acceptance criteria

- No critical or serious axe violations on the audited routes and states.
- No nested interactive controls or unnamed focusable elements in the accessibility tree.
- A complete keyboard pass has a logical order, visible non-overlapping focus, and no traps.
- Dialogs and mobile sheets announce their name, trap focus while open, close with Escape, and restore focus to their trigger.
- Text meets WCAG 2.2 AA contrast; interactive targets meet WCAG 2.2 target-size requirements.
- Lint, component tests, Playwright accessibility tests, builds, and the final manual desktop/mobile pass succeed.
