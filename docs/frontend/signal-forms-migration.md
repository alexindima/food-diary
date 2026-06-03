# Signal Forms Migration

Angular Signal Forms are available from `@angular/forms/signals` in Angular 22. Migrate incrementally and prefer small, behavior-preserving batches.

The official Angular docs currently still mark most Signal Forms APIs as experimental. Keep migrations narrow and verify each batch with focused tests plus build/lint.

## Current Status

- Baseline date: 2026-06-04.
- Migrated Signal Forms: 4 forms.
- Signal Forms files: 8.
- Remaining legacy Reactive Forms surface: 158 files.

Tracker patterns:

- Legacy Reactive Forms: `\bFormControl\b|\bFormGroup\b|\bFormArray\b|\bFormBuilder\b|\bNonNullableFormBuilder\b|\[formGroup\]|formControlName|\[formControl\]|ReactiveFormsModule`
- Signal Forms: `@angular/forms/signals|\[formField\]`

## Migrated

- `FoodDiary.Web.Client/projects/fooddiary-admin/src/app/features/admin-users/dialogs/admin-user-edit-dialog.ts`
- `FoodDiary.Web.Client/projects/fooddiary-admin/src/app/features/admin-users/dialogs/admin-user-edit-dialog.html`
- `FoodDiary.Web.Client/projects/fooddiary-admin/src/app/features/admin-users/dialogs/admin-user-impersonation-dialog.ts`
- `FoodDiary.Web.Client/projects/fooddiary-admin/src/app/features/admin-users/dialogs/admin-user-impersonation-dialog.html`
- `FoodDiary.Web.Client/src/app/features/goals/dialogs/calorie-goal-dialog/calorie-goal-dialog.ts`
- `FoodDiary.Web.Client/src/app/features/goals/dialogs/calorie-goal-dialog/calorie-goal-dialog.html`
- `FoodDiary.Web.Client/src/app/features/explore/dialogs/report-dialog/report-dialog.ts`
- `FoodDiary.Web.Client/src/app/features/explore/dialogs/report-dialog/report-dialog.html`

## Next Candidates

- `FoodDiary.Web.Client/src/app/features/explore/components/comments/recipe-comments.ts`
- `FoodDiary.Web.Client/src/app/features/meals/pages/list/meal-list-filters-dialog.ts`

## Defer Until Patterns Are Proven

- Forms with `FormArray`.
- Forms using `setValidators`, `addValidators`, `removeValidators`, or `clearValidators`.
- Forms using `enable()` / `disable()`.
- Forms using `setErrors()` / `markAsPending()`.
- `fd-ui-kit` ControlValueAccessor components and forms depending on them.

## Migration Notes

- Use `signal()` for the form model and `form()` for the field tree.
- Import `FormField` instead of `ReactiveFormsModule` when a component only binds native controls through `[formField]`.
- `FormField` can interoperate with `ControlValueAccessor` components for backwards compatibility. Treat each `fd-ui-*` form migration as a small pilot until UI-kit-specific patterns are proven.
- Keep custom array-style checkbox state explicit until a stable local pattern exists; `[formField]` does not cover multiple checkbox arrays directly.
- Update this file after each batch with migrated and remaining counts.
