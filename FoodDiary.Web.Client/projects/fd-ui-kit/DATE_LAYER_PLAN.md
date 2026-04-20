# Date Layer Plan

## Goal
- Remove direct Angular Material datepicker/input coupling from the UI kit.
- Replace the current date stack with design-system primitives that match the product's visuals and data contracts.
- Keep ISO-friendly values and explicit keyboard/accessibility behavior.

## Current State

The current date layer is split across four components:

- `fd-ui-date-input`
- `fd-ui-datetime-input`
- `fd-ui-date-range-input`
- `fd-ui-date-picker-button`

Current issues:

- `fd-ui-date-input` and `fd-ui-datetime-input` still depend on `MatDatepickerModule`, `MatNativeDateModule`, and `MatInputModule`.
- `fd-ui-date-picker-button` is only a toolbar trigger around a hidden Material datepicker input.
- `fd-ui-date-range-input` is a composition wrapper over two `fd-ui-date-input` instances, so it inherits the same Material dependency.
- The API is fragmented: field, range, and toolbar-selection use different contracts even though they all represent the same date-selection layer.

Current usage in the app:

- `fd-ui-date-input`
  - cycle tracking
  - profile
  - weight history
  - waist history
  - meal manage
- `fd-ui-date-range-input`
  - period filter
  - meal list filters
- `fd-ui-date-picker-button`
  - dashboard header date selector

## Recommendation

Yes, it makes sense to write our own datepicker.

Reasoning:

- Date selection is product-critical and visible in several flows.
- The current Material-backed implementation is already wrapped by our design system, so replacing internals will not create broad app churn.
- A custom datepicker will give us tighter control over:
  - visuals
  - compact layouts
  - toolbar/dashboard usage
  - ISO value handling
  - mobile behavior
  - future range selection UX

But it should be built as a small date system, not as one large component.

## Target Architecture

### Core primitives

1. `fd-ui-calendar`
- Inline month view.
- Responsible for:
  - month navigation
  - day grid rendering
  - disabled dates
  - min/max dates
  - selected day
  - keyboard navigation
- No field chrome and no overlay logic.

2. `fd-ui-date-picker`
- Overlay/popover wrapper around `fd-ui-calendar`.
- Responsible for:
  - anchored overlay
  - open/close state
  - focus restore
  - outside click / escape handling
  - optional presets later

3. `fd-ui-date-field`
- Standard field for single date values.
- Uses `fd-ui-input`-style shell and opens `fd-ui-date-picker`.
- Value contract:
  - external value: `string | null`
  - format: `YYYY-MM-DD`

4. `fd-ui-date-time-field`
- Reuses `fd-ui-date-field` plus time input segment.
- Value contract:
  - external value: `string | null`
  - format: `YYYY-MM-DDTHH:mm`

5. `fd-ui-date-range-field`
- Uses two single-date values but behaves as one control.
- Value contract:
  - external value: `{ start: Date | null; end: Date | null }`
  - or a new explicit range interface if we want to normalize later

### Optional convenience wrappers

6. `fd-ui-date-picker-button`
- Keep this public API for dashboard/toolbars.
- Reimplement it on top of `fd-ui-date-picker` instead of Material.
- This should be a thin convenience wrapper, not a separate date system.

## API Direction

### `fd-ui-date-field`

Suggested v1 inputs:

- `id`
- `label`
- `placeholder`
- `error`
- `required`
- `disabled`
- `size`
- `min`
- `max`
- `ariaLabel`

Suggested v1 model contract:

- `ControlValueAccessor`
- emits and accepts `YYYY-MM-DD` strings

### `fd-ui-calendar`

Suggested v1 inputs:

- `value: Date | null`
- `displayMonth: Date`
- `min: Date | null`
- `max: Date | null`
- `disabledDates?: (date: Date) => boolean`
- `weekStartsOn?: 0 | 1`

Suggested v1 outputs:

- `valueChange`
- `displayMonthChange`

### `fd-ui-date-picker`

Suggested v1 responsibilities:

- anchor to trigger
- render overlay panel
- host `fd-ui-calendar`
- expose `open`, `close`, `toggle`

## UX Rules

- Single-date picker should default to a compact one-month view.
- Selected day should be obvious and high-contrast.
- Today's date should have a distinct but non-competing marker.
- Range mode should support hover-preview later, but that is not required for v1.
- Toolbar usage should open quickly and feel lightweight.
- Field usage should work with both mouse and keyboard.
- Important value should never be hidden only in the overlay.

## Accessibility Rules

- Calendar grid should use roving tabindex.
- Support:
  - `ArrowLeft`
  - `ArrowRight`
  - `ArrowUp`
  - `ArrowDown`
  - `Home`
  - `End`
  - `PageUp`
  - `PageDown`
  - `Enter`
  - `Space`
  - `Escape`
- Overlay must restore focus to trigger on close.
- Field must expose correct `aria-invalid`, `aria-describedby`, and label association.
- Date button variant must always have an accessible name.

## Implementation Order

### Phase 1
- Build `fd-ui-calendar`.
- Build `fd-ui-date-picker` with CDK overlay.
- Reimplement `fd-ui-date-picker-button` on top of them.

Reason:
- This replaces the dashboard use case quickly.
- It creates the reusable popup foundation before touching form fields.

### Phase 2
- Build `fd-ui-date-field`.
- Migrate existing `fd-ui-date-input` usages to it.
- Keep `fd-ui-date-input` temporarily as a compatibility shell or deprecate it.

### Phase 3
- Build `fd-ui-date-time-field`.
- Replace `fd-ui-datetime-input`.

### Phase 4
- Rework `fd-ui-date-range-input` into `fd-ui-date-range-field`.
- Decide whether to keep the current range value contract or normalize it.

## Migration Notes

- App code should migrate to the new primitives without changing domain data shapes unless necessary.
- Keep the current ISO string contracts where they already exist.
- Avoid introducing `Date` objects into feature state unless the feature already uses them.
- Remove datepicker exports from `projects/fd-ui-kit/src/lib/material/index.ts` after migration is complete.

## Decision

The preferred path is:

1. Build our own calendar + picker stack in the design system.
2. Keep the current app-facing contracts as stable as possible.
3. Migrate dashboard first, then form fields, then range/datetime.

This gives us more flexibility than Material and keeps the migration controlled.
