# Signal Forms Migration

Angular Signal Forms are available from `@angular/forms/signals` in Angular 22. Migrate incrementally and prefer small, behavior-preserving batches.

The official Angular docs currently still mark most Signal Forms APIs as experimental. Keep migrations narrow and verify each batch with focused tests plus build/lint.

## Current Status

- Baseline date: 2026-06-04.
- Migrated Signal Forms: 15 forms.
- Signal Forms files: 34.
- Remaining legacy Reactive Forms surface: 129 files.

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
- `FoodDiary.Web.Client/src/app/features/explore/components/comments/recipe-comments.ts`
- `FoodDiary.Web.Client/src/app/features/explore/components/comments/recipe-comments.html`
- `FoodDiary.Web.Client/src/app/features/meals/pages/list/meal-list-filters-dialog/meal-list-filters-dialog.ts`
- `FoodDiary.Web.Client/src/app/features/meals/pages/list/meal-list-filters-dialog/meal-list-filters-dialog.html`
- `FoodDiary.Web.Client/projects/fooddiary-admin/src/app/features/admin-lessons/dialogs/admin-lesson-edit-dialog.ts`
- `FoodDiary.Web.Client/projects/fooddiary-admin/src/app/features/admin-lessons/dialogs/admin-lesson-edit-dialog.html`
- `FoodDiary.Web.Client/src/app/features/recipes/dialogs/recipe-select-dialog/recipe-select-dialog.ts`
- `FoodDiary.Web.Client/src/app/features/recipes/dialogs/recipe-select-dialog/recipe-select-dialog.html`
- `FoodDiary.Web.Client/src/app/features/explore/pages/explore/explore-page.ts`
- `FoodDiary.Web.Client/src/app/features/explore/pages/explore/explore-page.html`
- `FoodDiary.Web.Client/src/app/features/products/lib/list/product-list.facade.ts`
- `FoodDiary.Web.Client/src/app/features/products/components/list/product-list-base/product-list-base.ts`
- `FoodDiary.Web.Client/src/app/features/products/components/list/product-list-base/product-list-base.html`
- `FoodDiary.Web.Client/src/app/features/products/pages/list/product-list-page.ts`
- `FoodDiary.Web.Client/src/app/features/products/dialogs/product-list-dialog/product-list-dialog.ts`
- `FoodDiary.Web.Client/src/app/features/products/dialogs/product-list-dialog/product-list-dialog.html`
- `FoodDiary.Web.Client/src/app/features/recipes/pages/list/recipe-list.ts`
- `FoodDiary.Web.Client/src/app/features/recipes/pages/list/recipe-list.html`
- `FoodDiary.Web.Client/src/app/features/meals/pages/list/meal-list.ts`
- `FoodDiary.Web.Client/src/app/features/shopping-lists/pages/shopping-list-page/shopping-list-page.ts`
- `FoodDiary.Web.Client/src/app/features/shopping-lists/pages/shopping-list-page/shopping-list-page.html`
- `FoodDiary.Web.Client/src/app/features/shopping-lists/pages/shopping-list-items-panel/shopping-list-items-panel.ts`
- `FoodDiary.Web.Client/src/app/features/shopping-lists/pages/shopping-list-items-panel/shopping-list-items-panel.html`
- `FoodDiary.Web.Client/src/app/features/shopping-lists/pages/shopping-list-manage-controls/shopping-list-manage-controls.ts`
- `FoodDiary.Web.Client/src/app/features/shopping-lists/pages/shopping-list-manage-controls/shopping-list-manage-controls.html`
- `FoodDiary.Web.Client/src/app/features/shopping-lists/lib/shopping-list-form.types.ts`

## Next Candidates

- Small dialogs/pages without `FormArray`.
- Forms backed by existing `fd-ui-*` CVA components when the submit/reset behavior is simple.

## Defer Until Patterns Are Proven

- Forms with `FormArray`.
- Forms using `setValidators`, `addValidators`, `removeValidators`, or `clearValidators`.
- Forms using `enable()` / `disable()`.
- Forms using `setErrors()` / `markAsPending()`.
- Complex `fd-ui-kit` ControlValueAccessor forms until UI-kit-specific patterns are proven.

## Migration Notes

- Use `signal()` for the form model and `form()` for the field tree.
- Import `FormField` instead of `ReactiveFormsModule` when a component only binds native controls through `[formField]`.
- `FormField` can interoperate with `ControlValueAccessor` components for backwards compatibility. Treat each `fd-ui-*` form migration as a small pilot until UI-kit-specific patterns are proven.
- Keep custom array-style checkbox state explicit until a stable local pattern exists; `[formField]` does not cover multiple checkbox arrays directly.
- Update this file after each batch with migrated and remaining counts.
