# Food Diary UI Kit

This document describes the shared UI components from `FoodDiary.Web.Client/projects/fd-ui-kit` and how to use them.
It is intended as a quick reference for feature work.

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
- `prefixIcon?: string` (material icon name)
- `suffixButtonIcon?: string` (material icon name)
- `suffixButtonAriaLabel?: string`

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
Dropdown select based on `MatMenu`.

**Inputs**
- `label?: string`
- `placeholder?: string`
- `error?: string | null`
- `required?: boolean`
- `size?: 'sm' | 'md' | 'lg'`
- `fillColor?: string | null`
- `options?: FdUiSelectOption<T>[]`

**Types**
```ts
export interface FdUiSelectOption<T = unknown> {
  value: T;
  label: string;
  hint?: string;
}
```

#### `fd-ui-date-input`
Material datepicker input (value as `YYYY-MM-DD`).

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

#### `fd-ui-nutrient-input`
Specialized numeric input for nutrition cards.

**Inputs**
- `label: string`
- `icon?: string` (material icon)
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

#### `fd-ui-button`
Primary button component.

**Inputs**
- `type?: 'button' | 'submit' | 'reset'`
- `variant?: 'primary' | 'secondary' | 'danger' | 'info' | 'ghost' | 'outline'`
- `fill?: 'solid' | 'outline' | 'text' | 'ghost'`
- `size?: 'xs' | 'sm' | 'md' | 'lg'`
- `icon?: string` (material icon)
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

### Navigation / UX

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

#### `fd-ui-menu`, `fd-ui-menu-item`, `fd-ui-menu-trigger`, `fd-ui-menu-divider`
Context menu components.

#### `fd-ui-loader`
Loading indicator.

#### `fd-ui-toast` (service)
Toast notifications service.

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
