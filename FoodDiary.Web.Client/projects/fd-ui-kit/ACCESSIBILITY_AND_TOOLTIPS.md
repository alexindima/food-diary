# Accessibility And Tooltips

This document defines the baseline rules for accessible names, icon-only actions, and tooltip usage in `fd-ui-kit` and app screens that consume it.

## Goals

- Keep interactive UI understandable without guesswork.
- Make icon-only actions safe for keyboard and screen reader users.
- Use tooltips as a helper layer, not as the only source of meaning.
- Keep repeated patterns consistent across the client apps.

## Core Rule

Every interactive element must have a clear accessible name.

That name should come from the most semantic source available, in this order:

1. Visible text content.
2. Associated visible label via `label` / `aria-labelledby`.
3. `aria-label` when no visible text label exists.

Do not add `aria-label` by default to every control. Use it only when the accessible name is otherwise missing or incomplete.

## Accessible Name Rules

### Use visible text when it already exists

Prefer the visible label for:

- text buttons;
- links;
- menu items;
- tabs;
- form fields with a visible label.

Examples:

- `Save`
- `Add goal`
- `Notifications`

These usually do not need `aria-label`.

### Use `aria-label` for icon-only controls

Any button, toggle, or actionable icon without visible text must define an accessible name with `aria-label` unless it is already named by `aria-labelledby`.

Examples:

- calendar button;
- filter button;
- microphone button;
- camera button;
- close button;
- expand/collapse icon button.

The label should describe the action, not the icon.

Good:

- `aria-label="Open calendar"`
- `aria-label="Start voice input"`
- `aria-label="Expand Nutrition section"`

Avoid:

- `aria-label="Calendar icon"`
- `aria-label="Microphone"`

### Prefer `aria-labelledby` for compound controls

If the control is named by nearby visible text already present in the DOM, prefer `aria-labelledby`.

Use this for:

- region actions tied to a card heading;
- switches named by a settings row label;
- grouped controls with a visible section title.

### Do not overwrite good visible labels without reason

If a button already says `Add goal`, do not add `aria-label="Create hydration goal"` unless there is a real accessibility reason to expose a different name.

Redundant `aria-label` can make the spoken output less consistent than the visible UI.

## Tooltip Rules

### When tooltips are required

Tooltips should be provided for icon-only interactive controls on desktop.

They must open on:

- pointer hover;
- keyboard focus.

They must close on:

- pointer leave;
- blur;
- `Escape`, when supported by the tooltip primitive.

### When tooltips are useful

Tooltips are recommended for:

- uncommon metrics or abbreviations;
- controls with ambiguous iconography;
- controls whose result is not obvious from the surrounding layout.

Examples:

- fasting protocol notation like `16:8`;
- micronutrient indicators;
- chart affordances;
- compact toolbar actions.

### When tooltips are unnecessary

Avoid tooltips for:

- buttons with clear visible text;
- navigation items with clear labels;
- large CTA blocks whose purpose is already explicit;
- content that is always visible inline.

### Do not hide important meaning in a tooltip

A tooltip is supplementary help. It must not be the only place where the user can learn:

- what a critical action does;
- why a state is empty;
- what is required to continue;
- the consequence of a destructive action.

If the explanation is important, keep it visible inline or use a popover, dialog, or help section.

### Keep tooltip content short

Tooltip text should be one short phrase or sentence.

Prefer:

- `Open calendar`
- `Add photo`
- `Daily water target progress`

Avoid long instructional copy inside a tooltip.

If the content needs more than one short sentence, use a popover or inline helper text instead.

Default placement should prefer `bottom` for action buttons and compact controls. Override per instance only when layout context makes another side clearly better.

## Mobile And Touch Behavior

Do not rely on hover for essential understanding.

For touch devices:

- icon-only controls still require an accessible name;
- essential context must remain available without hover;
- long explanations should move to inline help or a click/tap-triggered popover.

## Decorative Icons

Icons that do not add meaning should be hidden from assistive technology.

Use `aria-hidden="true"` for decorative icons inside labeled controls and content.

Examples:

- leading icon in a text button;
- status ornament next to a visible heading;
- purely decorative card glyph.

## Writing Rules For Labels And Tooltips

- Prefer verbs for actions: `Open calendar`, `Add water`, `Start recording`.
- Prefer nouns only for pure objects or destinations: `Notifications`, `Dashboard`.
- Match the visible UI terminology.
- Keep wording concise and unambiguous.
- Do not expose implementation details like `button`, `icon`, or `control` in the label text.

## Default UI-Kit Policy

Apply these defaults across shared components:

- Text button: visible text is the accessible name; no extra tooltip by default.
- Icon-only button: requires `ariaLabel` and should show `fdUiHint` on hover and focus.
- Input with visible label: use the visible label; do not duplicate with `aria-label`.
- Input without visible label: add `aria-label` or `aria-labelledby`, but prefer a visible label when practical.
- Toggle in a labeled row: prefer naming from the row label.
- Decorative icon: hide from assistive technology.

## Review Checklist

Before shipping a new control or screen, verify:

1. Can the purpose be understood without relying on hover?
2. Does every interactive element have a clear accessible name?
3. Do all icon-only buttons have `aria-label`?
4. Do icon-only buttons expose a tooltip on hover and keyboard focus?
5. Is any important explanation hidden only inside a tooltip?
6. Are decorative icons marked with `aria-hidden="true"`?
7. Does the spoken name match the visible wording closely enough?

## Examples For Food Diary

Use tooltip and `aria-label`:

- calendar action in page toolbar;
- filter/settings icon in page toolbar;
- camera action in meal input;
- microphone action in meal input;
- standalone collapse/expand icon buttons.

Usually no tooltip needed:

- `Notifications` button with visible text;
- `Add manually`;
- `Add goal`;
- sidebar items with icon plus text.

Prefer inline text or popover instead of tooltip:

- explanation of fasting cycles;
- empty-state guidance for water and cycle cards;
- nutrition methodology details;
- anything that requires more than a short phrase.
