# Frontend UX Playbook

This document defines the default UX patterns for async states in the client app.
Use it when building new pages or normalizing existing flows.

## Core Rules

### Page Bootstrap
- Use skeletons for page-level loading when the final layout is already known.
- Prefer layout-matching placeholders over plain `Loading...` text.
- Use section skeletons for dashboards, cards, charts, and lists that hydrate independently.

### Long GET Requests
- Use the global top loader for long-running read requests.
- Do not rely on the top loader as the only feedback when a section can show a local skeleton or placeholder.
- Silent background sync requests should opt out of the global top loader.

### Mutations
- Use button `loading` for user-triggered mutations.
- Button loading is the default for submit, save, create, delete, resend, retry, and confirm actions.
- Keep the button width stable while loading.

### Autosave
- Do not use button loading for autosave-only flows.
- Use a lightweight inline status instead, such as `Saving`, `Saved`, or `Sync failed`.
- Autosave errors must be visible without requiring the user to inspect the network tab.

### Error States
- Use `fd-error-state` for page or section load failures when retry is possible.
- Prefer inline form errors for validation failures.
- Prefer toast only for short recoverable action failures that do not need persistent context.

### Empty States
- Distinguish between:
  - empty state: the user has no data yet;
  - no-results state: filters/search returned nothing.
- Empty states should explain what happened and, when appropriate, offer a primary action.

### Success Feedback
- Use toast for completed actions that keep the user on the same screen and do not visibly prove success on their own.
- Do not stack redundant success patterns. If the updated UI already clearly shows success, avoid extra toast noise.

### Destructive Actions
- Use a confirm dialog before destructive or hard-to-reverse actions.
- The confirm action itself should show button loading if it performs a request.

### Settings and Toggles
- For simple toggles, disable the control while the update is in flight.
- For multi-field settings sections, prefer either:
  - explicit save button with button loading; or
  - autosave with a visible inline status.
- Avoid mixing unrelated save patterns inside the same settings section unless there is a strong product reason.

## Default Patterns by Screen Type

### List Pages
- header
- filters/search
- skeleton grid while loading
- `fd-error-state` on load error
- dedicated empty state
- dedicated no-results state
- pagination or load-more feedback

### Form Pages
- field validation errors inline
- primary submit action uses button loading
- page load errors use `fd-error-state`
- avoid plain text-only loading states if the form shape is known

### Dashboard Pages
- defer heavy sections
- use section-level skeletons or loaders
- keep page responsive even when one widget is still hydrating

### Settings Pages
- keep save behavior consistent within the page
- if autosave is used, expose status clearly
- if explicit save is used, primary actions must show button loading

## Anti-Patterns
- Plain `Loading...` text as the only state on a structured page
- Global top loader as the only feedback for a clicked button
- Same visual treatment for empty state and no-results state
- Hidden autosave failures
- Destructive actions without confirm or pending feedback
