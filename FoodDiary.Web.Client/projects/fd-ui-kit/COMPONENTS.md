# Food Diary UI Kit

This document describes the shared UI components from `FoodDiary.Web.Client/projects/fd-ui-kit` and how to use them.
It is intended as a quick reference for feature work.

For tooltip and accessible-name rules, see `ACCESSIBILITY_AND_TOOLTIPS.md`.

## Usage

Prefer importing from the barrel:

```ts
import {
  FdUiInputComponent,
  FdUiSelectComponent,
  FdUiButtonComponent,
} from 'fd-ui-kit';
```

All components are **standalone** and can be listed in a component `imports` array.
Most form controls implement **ControlValueAccessor** and work with `formControlName`/`ngModel`.

## Field sizes

Most inputs accept `size` (`'sm' | 'md' | 'lg'`). The default is `md`.

## Components

### Inputs

#### `fd-ui-input`
Universal single-line input.

**Inputs**
- `label?: string`
- `placeholder?: string`
- `type?: 'text' | 'number' | 'password' | 'email' | 'tel' | 'date' | 'datetime-local' | 'time'` (default `text`)
- `error?: string | null`
- `required?: boolean`
- `readonly?: boolean`
- `maxLength?: number`
- `step?: string | number`
- `size?: 'sm' | 'md' | 'lg'`
- `fillColor?: string | null` (custom background)
- `prefixIcon?: string` (icon name)
- `suffixButtonIcon?: string` (icon name)
- `suffixButtonAriaLabel?: string`

**CSS variables**
- `--fd-input-height`
- `--fd-input-radius`
- `--fd-input-border`
- `--fd-input-hover-border`
- `--fd-input-focus-border`
- `--fd-input-focus-shadow`
- `--fd-input-surface`
- `--fd-input-label-bg`
- `--fd-input-control-padding-left`
- `--fd-input-control-padding-right`
- `--fd-input-control-font-size`

**Outputs**
- `suffixButtonClicked`

**Example**
```html
<fd-ui-input
  label="Name"
  placeholder="Enter name"
  [required]="true"
  [suffixButtonIcon]="'close'"
  (suffixButtonClicked)="clear()"
  formControlName="name"
></fd-ui-input>
```

#### `fd-ui-textarea`
Multi-line input.

**Inputs**
- `label?: string`
- `placeholder?: string`
- `error?: string | null`
- `required?: boolean`
- `readonly?: boolean`
- `rows?: number` (default `4`)
- `maxLength?: number`
- `size?: 'sm' | 'md' | 'lg'`
- `fillColor?: string | null`

#### `fd-ui-select`
Dropdown select based on the design-system menu/overlay primitives.

**Inputs**
- `label?: string`
- `placeholder?: string`
- `error?: string | null`
- `required?: boolean`
- `size?: 'sm' | 'md' | 'lg'`
- `fillColor?: string | null`
- `options?: FdUiSelectOption<T>[]`

**CSS variables**
- `--fd-select-height`
- `--fd-select-radius`
- `--fd-select-border`
- `--fd-select-surface`

**Types**
```ts
export interface FdUiSelectOption<T = unknown> {
  value: T;
  label: string;
  hint?: string;
}
```

#### `fd-ui-calendar`
Inline calendar primitive for custom date pickers and date popovers.

**Inputs**
- `value?: Date | null`
- `displayMonth?: Date | null`
- `min?: Date | null`
- `max?: Date | null`
- `weekStartsOn?: 0 | 1`

#### `fd-ui-date-input`
Date input (value as `YYYY-MM-DD`).

**Inputs**
- `label?: string`
- `placeholder?: string`
- `error?: string | null`
- `required?: boolean`
- `size?: 'sm' | 'md' | 'lg'`

#### `fd-ui-time-input`
Time input (`HH:mm`).

**Inputs**
- `label?: string`
- `placeholder?: string`
- `error?: string | null`
- `required?: boolean`
- `size?: 'sm' | 'md' | 'lg'`

#### `fd-ui-datetime-input`
Date + time input (value as `YYYY-MM-DDTHH:mm`).

**Inputs**
- `label?: string`
- `placeholder?: string`
- `error?: string | null`
- `required?: boolean`
- `size?: 'sm' | 'md' | 'lg'`

#### `fd-ui-date-range-input`
Date range wrapper with two date inputs.

**Inputs**
- `startLabel?: string`
- `endLabel?: string`
- `startPlaceholder?: string`
- `endPlaceholder?: string`
- `size?: 'sm' | 'md' | 'lg'`

**Type**
```ts
export type FdUiDateRangeValue = { start: Date | null; end: Date | null };
```

#### `fd-ui-date-picker-button`
Compact date trigger with overlay calendar.

**Inputs**
- `value?: Date | null`
- `min?: Date | null`
- `max?: Date | null`
- `ariaLabel?: string`
- `hint?: string | null`
- `icon?: string`

#### `fd-ui-nutrient-input`
Specialized numeric input for nutrition cards.

**Inputs**
- `label: string`
- `icon?: string` (icon name)
- `placeholder?: string` (default `0`)
- `name?: string`
- `type?: 'text' | 'number'` (default `number`)
- `step?: string | number`
- `min?: string | number`
- `max?: string | number`
- `required?: boolean`
- `readonly?: boolean`
- `error?: string | null`
- `tintColor?: string` (background tint)
- `textColor?: string` (label/value color)
- `size?: 'sm' | 'md' | 'lg'`
- `variant?: 'tinted' | 'outline'`
- `labelUppercase?: boolean`
- `valueAlign?: 'center' | 'left'`

### Buttons

#### `fd-ui-icon`
Shared ligature icon wrapper used by the design system.

**Inputs**
- `name: string`
- `size?: 'sm' | 'md' | 'lg' | 'xl' | number`
- `decorative?: boolean` (default `true`)
- `ariaLabel?: string | null`
- `fontSet?: string | null`

#### `fd-ui-button`
Primary button component.

**Inputs**
- `type?: 'button' | 'submit' | 'reset'`
- `variant?: 'primary' | 'secondary' | 'danger' | 'info' | 'ghost' | 'outline'`
- `fill?: 'solid' | 'outline' | 'text' | 'ghost'`
- `appearance?: 'default' | 'toolbar' | 'card-action' | 'dashed' | 'plain-icon' | 'brand-action' | 'chip'`
- `size?: 'xs' | 'sm' | 'md' | 'lg'`
- `icon?: string` (icon name)
- `iconSize?: 'xs' | 'sm' | 'md' | 'lg' | 'xl'`
- `disabled?: boolean`
- `fullWidth?: boolean`
- `ariaLabel?: string`

### Cards

#### `fd-ui-card`
Base container with consistent radius and padding.

**Inputs**
- `subtle?: boolean`

#### `fd-ui-card-actions` (directive)
Use inside `fd-ui-card` for action rows.

#### `fd-ui-entity-card`
Entity tile/card (e.g. product/recipe cards).

**Inputs**
- `fallbackImage?: string`

#### `fd-ui-entity-card-header` (directive)
Header slot for `fd-ui-entity-card`.

### Dialogs

#### `fd-ui-dialog`
Dialog wrapper used with `FdUiDialogService`.

**Inputs**
- `dismissible?: boolean`

#### `fd-ui-dialog-shell`
Layout wrapper for dialog content.

**Inputs**
- `dismissible?: boolean`
- `flush?: boolean`

#### `fd-ui-confirm-dialog`
Standard confirm dialog (uses dialog service).

### Form helpers

#### `fd-ui-form-error`
Displays validation errors.

**Inputs**
- `showOnDirty?: boolean`

### Selection

#### `fd-ui-checkbox`
Checkbox with label + hint.

**Inputs**
- `label?: string`
- `hint?: string`
- `disabled?: boolean` (model)

#### `fd-ui-radio-group`
Radio group for a list of options.

**Inputs**
- `label?: string`
- `hint?: string`
- `error?: string | null`
- `required?: boolean`
- `orientation?: 'vertical' | 'horizontal'`
- `options?: FdUiRadioOption<T>[]`

**Types**
```ts
export interface FdUiRadioOption<T = unknown> {
  label: string;
  value: T;
  description?: string;
}
```

#### `fd-ui-segmented-toggle`
Segmented toggle control.

#### `fd-ui-chip-select`
Compact multi-select chip group for filters, tags, and symptom pickers.

**Inputs**
- `options?: FdUiChipSelectOption[]`
- `selectedValues?: string[]`
- `ariaLabel?: string | null`
- `size?: 'sm' | 'md'`

**Types**
```ts
export interface FdUiChipSelectOption {
  value: string;
  label: string;
  disabled?: boolean;
  ariaLabel?: string | null;
  hint?: string | null;
}
```

#### `fd-ui-emoji-picker`
Compact emoji-based single-choice picker.

**Inputs**
- `options?: FdUiEmojiPickerOption[]`
- `selectedValue?: string | number | null`
- `ariaLabel?: string | null`
- `size?: 'sm' | 'md'`
- `fullWidth?: boolean`
- `showLabels?: boolean`
- `showDescriptions?: boolean`

**Types**
```ts
export interface FdUiEmojiPickerOption<T = string | number> {
  value: T;
  emoji: string;
  label?: string;
  description?: string;
  ariaLabel?: string;
  hint?: string;
  disabled?: boolean;
}
```

#### `fd-ui-switch`
Compact boolean switch for settings and permission rows.

**Inputs**
- `checked?: boolean`
- `disabled?: boolean`
- `ariaLabel?: string`
- `onLabel?: string`
- `offLabel?: string`
- `showStateLabel?: boolean`

### Navigation / UX

#### `fdUiHint`
Tooltip directive for short helper text attached to an existing element.

Use for:

- icon-only buttons;
- compact toolbar actions;
- short labels for ambiguous controls.

Do not use for long or interactive content. Prefer a popover or inline help for those cases.

**Inputs**
- `fdUiHint: string | TemplateRef | null`
- `fdUiHintHtml?: boolean`
- `fdUiHintContext?: Record<string, unknown> | null`
- `fdUiHintShowDelay?: number` (default `500`)
- `fdUiHintFocusShowDelay?: number` default `0`
- `fdUiHintHideDelay?: number` (default `0`)
- `fdUiHintPosition?: 'top' | 'bottom' | 'left' | 'right'` (default `bottom`)
- `fdUiHintDisabled?: boolean`

#### `fd-ui-tabs`
Tabs component.

#### `fd-ui-pagination`
Pagination control.

**Inputs**
- `length?: number`
- `pageSize?: number`
- `pageIndex?: number`

### Misc

#### `fd-ui-accent-surface`
Toned surface container.

**Inputs**
- `active?: boolean`
- `tinted?: boolean`

#### `fd-ui-satiety-scale`
Hunger/satiety scale widget.

**Inputs**
- `required?: boolean`

#### `fd-ui-inline-alert`
Inline alert/banner for actionable warnings, info states, or success notices.

**Inputs**
- `appearance?: 'alert' | 'notice'`
- `severity?: 'info' | 'warning' | 'success' | 'danger'`
- `title?: string`
- `message?: string`
- `primaryActionLabel?: string | null`
- `secondaryActionLabel?: string | null`
- `dismissible?: boolean`

**Outputs**
- `primaryAction`
- `secondaryAction`
- `dismiss`

#### `fd-ui-menu`, `fd-ui-menu-item`, `fd-ui-menu-trigger`, `fd-ui-menu-divider`
Context menu components.

#### `fd-ui-loader`
Loading indicator.

#### `fd-ui-toast-host`
Global toast host. Mount once near the app root.

#### `fd-ui-toast` (service)
Toast notifications service.

**Methods**
- `open(message, options?)`
- `success(message, options?)`
- `error(message, options?)`
- `info(message, options?)`
- `dismissAll()`

**Usage guide**
- Use `success()` for completed user actions that keep the user on the same screen: save, add, delete, toggle on/off.
- Use `info()` for neutral background feedback: scheduled work, sync started/completed, capability status.
- Use `error()` for short recoverable failures where the user may retry without extra context.
- Keep toast copy to one short sentence. If the message needs explanation or field-level correction, use inline validation, a banner, or a dialog instead.
- Do not show a toast when the updated UI already makes the result obvious.

---

## Conventions

- Prefer `fd-ui-*` components over Angular Material inputs directly.
- Keep labels short and in Title Case (or per i18n).
- Use `error` to display validation messages (no inline HTML errors).
- For new components, add them to `projects/fd-ui-kit/src/lib/index.ts` so they are available via the `fd-ui-kit` barrel.

## Where to add new components

Place new UI kit components under:
`FoodDiary.Web.Client/projects/fd-ui-kit/src/lib/<component-name>/`

Update exports in:
- `FoodDiary.Web.Client/projects/fd-ui-kit/src/lib/index.ts`

If the component should be available via `FdUiKitModule`, add it to:
- `FoodDiary.Web.Client/projects/fd-ui-kit/src/lib/fd-ui-kit.module.ts`
