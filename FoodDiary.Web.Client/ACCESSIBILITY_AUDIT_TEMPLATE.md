# Accessibility Audit Template

Use this template for keyboard-only and screen-reader-oriented manual passes on key screens.
Fill one section per screen and keep notes short and concrete.

## How To Run The Audit

For each screen, test three passes:

1. Keyboard only
2. Keyboard plus visible focus
3. Screen-reader logic without a full reader

Core checks for every screen:

- First interactive element is reachable with `Tab`
- Tab order is stable and predictable
- `Enter` and `Space` trigger the expected actions
- Overlay surfaces close on `Escape`
- Focus returns to the trigger after closing an overlay
- All critical actions have clear accessible names
- Expanded sections expose correct `aria-expanded`
- Busy states are visible and do not leave dead controls
- Errors are exposed as alerts or clear inline feedback
- Success feedback is not toast-only for critical flows

Severity guide:

- `P0`: blocks the flow, breaks keyboard access, missing critical name, broken focus/escape
- `P1`: major usability/accessibility issue, but the scenario still works
- `P2`: polish or consistency issue

## Audit Log

### Screen

- Name:
- Route:
- Date:
- Auditor:

### Coverage

- Keyboard only: `Not started`
- Focus visibility: `Not started`
- Screen-reader logic: `Not started`

### Passes

- Pass:
- Pass:
- Pass:

### Findings

- Severity:
  ID:
  Area:
  Issue:
  Repro:
  Expected:
  Notes:

- Severity:
  ID:
  Area:
  Issue:
  Repro:
  Expected:
  Notes:

### Decision

- Status: `Pass` / `Pass with follow-ups` / `Needs fixes`
- Follow-up owner:
- Follow-up notes:

## Suggested Screen Order

1. `dashboard`
2. `meals list`
3. `meal manage`
4. `products list/detail/manage`
5. `recipes list/manage`
6. `fasting`
7. `statistics`
8. `profile`
9. `auth`
10. `notifications dialog`
