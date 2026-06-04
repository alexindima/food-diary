# Signal Forms Migration

Angular Signal Forms are available from `@angular/forms/signals` in Angular 22. Migrate incrementally and prefer small, behavior-preserving batches.

The official Angular docs currently still mark most Signal Forms APIs as experimental. Keep migrations narrow and verify each batch with focused tests plus build/lint.

## Current Status

- Baseline date: 2026-06-04.
- Migrated Signal Forms: 51 forms.
- Signal Forms files: 121.
- Remaining legacy Reactive Forms surface: 18 files.

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
- `FoodDiary.Web.Client/projects/fooddiary-admin/src/app/features/admin-email-templates/dialogs/admin-email-template-edit-dialog.ts`
- `FoodDiary.Web.Client/projects/fooddiary-admin/src/app/features/admin-email-templates/dialogs/admin-email-template-edit-dialog.html`
- `FoodDiary.Web.Client/src/app/features/dietologist/pages/client-dashboard/client-dashboard.ts`
- `FoodDiary.Web.Client/src/app/features/dietologist/pages/client-dashboard/client-dashboard.html`
- `FoodDiary.Web.Client/src/app/features/cycle-tracking/lib/cycle-tracking.facade.ts`
- `FoodDiary.Web.Client/src/app/features/cycle-tracking/pages/cycle-tracking-page.ts`
- `FoodDiary.Web.Client/src/app/features/cycle-tracking/pages/cycle-tracking-page.html`
- `FoodDiary.Web.Client/projects/fd-ui-kit/src/lib/date-input/fd-ui-date-input.ts`
- `FoodDiary.Web.Client/projects/fd-ui-kit/src/lib/date-input/fd-ui-date-input.html`
- `FoodDiary.Web.Client/projects/fd-ui-kit/src/lib/date-range-input/fd-ui-date-range-input.ts`
- `FoodDiary.Web.Client/projects/fd-ui-kit/src/lib/date-range-input/fd-ui-date-range-input.html`
- `FoodDiary.Web.Client/projects/fd-ui-kit/src/lib/datetime-input/fd-ui-datetime-input.ts`
- `FoodDiary.Web.Client/projects/fd-ui-kit/src/lib/datetime-input/fd-ui-datetime-input.html`
- `FoodDiary.Web.Client/src/app/components/shared/period-filter/period-filter.ts`
- `FoodDiary.Web.Client/src/app/components/shared/period-filter/period-filter.html`
- `FoodDiary.Web.Client/src/app/features/statistics/lib/statistics.facade.ts`
- `FoodDiary.Web.Client/src/app/features/statistics/pages/statistics.ts`
- `FoodDiary.Web.Client/src/app/features/statistics/pages/statistics.html`
- `FoodDiary.Web.Client/src/app/features/weight-history/lib/weight-history.facade.ts`
- `FoodDiary.Web.Client/src/app/features/weight-history/pages/weight-history-page/weight-history-page.ts`
- `FoodDiary.Web.Client/src/app/features/weight-history/pages/weight-history-page/weight-history-page.html`
- `FoodDiary.Web.Client/src/app/features/weight-history/components/weight-history-form-card/weight-history-form-card.ts`
- `FoodDiary.Web.Client/src/app/features/weight-history/components/weight-history-form-card/weight-history-form-card.html`
- `FoodDiary.Web.Client/src/app/features/weight-history/components/weight-history-goal-card/weight-history-goal-card.ts`
- `FoodDiary.Web.Client/src/app/features/weight-history/components/weight-history-goal-card/weight-history-goal-card.html`
- `FoodDiary.Web.Client/src/app/features/waist-history/lib/waist-history.facade.ts`
- `FoodDiary.Web.Client/src/app/features/waist-history/pages/waist-history-page/waist-history-page.ts`
- `FoodDiary.Web.Client/src/app/features/waist-history/pages/waist-history-page/waist-history-page.html`
- `FoodDiary.Web.Client/src/app/features/waist-history/components/waist-history-form-card/waist-history-form-card.ts`
- `FoodDiary.Web.Client/src/app/features/waist-history/components/waist-history-form-card/waist-history-form-card.html`
- `FoodDiary.Web.Client/src/app/features/waist-history/components/waist-history-goal-card/waist-history-goal-card.ts`
- `FoodDiary.Web.Client/src/app/features/waist-history/components/waist-history-goal-card/waist-history-goal-card.html`
- `FoodDiary.Web.Client/src/app/features/products/dialogs/product-ai-recognition-dialog/product-ai-recognition-dialog.ts`
- `FoodDiary.Web.Client/src/app/features/products/dialogs/product-ai-recognition-dialog/product-ai-recognition-dialog.html`
- `FoodDiary.Web.Client/src/app/features/products/dialogs/product-ai-recognition-dialog/product-ai-recognition-dialog.types.ts`
- `FoodDiary.Web.Client/src/app/features/products/dialogs/product-ai-recognition-dialog/product-ai-recognition-lib/product-ai-recognition.helpers.ts`
- `FoodDiary.Web.Client/src/app/features/products/dialogs/product-ai-recognition-dialog/product-ai-recognition-result/product-ai-recognition-result.ts`
- `FoodDiary.Web.Client/src/app/features/products/dialogs/product-ai-recognition-dialog/product-ai-recognition-result/product-ai-recognition-result.html`
- `FoodDiary.Web.Client/src/app/features/auth/components/auth/auth.ts`
- `FoodDiary.Web.Client/src/app/features/auth/components/auth/auth.html`
- `FoodDiary.Web.Client/src/app/features/auth/components/auth/auth-lib/auth.types.ts`
- `FoodDiary.Web.Client/src/app/features/auth/components/auth/auth-lib/auth-form.factory.ts`
- `FoodDiary.Web.Client/src/app/features/auth/components/auth/auth-lib/auth-form.manager.ts`
- `FoodDiary.Web.Client/src/app/features/auth/components/auth/auth-login-form/auth-login-form.ts`
- `FoodDiary.Web.Client/src/app/features/auth/components/auth/auth-login-form/auth-login-form.html`
- `FoodDiary.Web.Client/src/app/features/auth/components/auth/auth-register-form/auth-register-form.ts`
- `FoodDiary.Web.Client/src/app/features/auth/components/auth/auth-register-form/auth-register-form.html`
- `FoodDiary.Web.Client/src/app/features/auth/components/auth/auth-register-fields/auth-register-fields.ts`
- `FoodDiary.Web.Client/src/app/features/auth/components/auth/auth-register-fields/auth-register-fields.html`
- `FoodDiary.Web.Client/src/app/features/auth/components/auth/auth-password-reset-form/auth-password-reset-form.ts`
- `FoodDiary.Web.Client/src/app/features/auth/components/auth/auth-password-reset-form/auth-password-reset-form.html`
- `FoodDiary.Web.Client/src/app/features/profile/dialogs/change-password-dialog/change-password-dialog.ts`
- `FoodDiary.Web.Client/src/app/features/profile/dialogs/change-password-dialog/change-password-dialog.html`
- `FoodDiary.Web.Client/src/app/features/auth/pages/password-reset/password-reset.ts`
- `FoodDiary.Web.Client/src/app/features/auth/pages/password-reset/password-reset.html`
- `FoodDiary.Web.Client/src/app/features/profile/pages/user-manage/user-manage.ts`
- `FoodDiary.Web.Client/src/app/features/profile/pages/user-manage/user-manage.html`
- `FoodDiary.Web.Client/src/app/features/profile/pages/user-manage/user-manage-lib/user-manage.types.ts`
- `FoodDiary.Web.Client/src/app/features/profile/pages/user-manage/user-manage-lib/user-manage-form.mapper.ts`
- `FoodDiary.Web.Client/src/app/features/profile/pages/user-manage/user-manage-lib/user-manage-dietologist-form.mapper.ts`
- `FoodDiary.Web.Client/src/app/features/profile/pages/user-manage-sections/account-card/user-manage-account-card.ts`
- `FoodDiary.Web.Client/src/app/features/profile/pages/user-manage-sections/account-card/user-manage-account-card.html`
- `FoodDiary.Web.Client/src/app/features/profile/pages/user-manage-sections/body-card/user-manage-body-card.ts`
- `FoodDiary.Web.Client/src/app/features/profile/pages/user-manage-sections/body-card/user-manage-body-card.html`
- `FoodDiary.Web.Client/src/app/features/profile/pages/user-manage-sections/dietologist-card/user-manage-dietologist-card.ts`
- `FoodDiary.Web.Client/src/app/features/profile/pages/user-manage-sections/dietologist-card/user-manage-dietologist-card.html`
- `FoodDiary.Web.Client/src/app/features/profile/pages/user-manage-sections/dietologist-summary/user-manage-dietologist-summary.ts`
- `FoodDiary.Web.Client/src/app/features/profile/pages/user-manage-sections/dietologist-summary/user-manage-dietologist-summary.html`
- `FoodDiary.Web.Client/src/app/components/shared/nutrition-editor/nutrition-editor.ts`
- `FoodDiary.Web.Client/src/app/components/shared/nutrition-editor/nutrition-editor.html`
- `FoodDiary.Web.Client/src/app/features/products/components/detail/product-detail/product-detail.ts`
- `FoodDiary.Web.Client/src/app/features/products/components/detail/product-detail/product-detail.html`
- `FoodDiary.Web.Client/src/app/features/products/components/detail/product-detail-lib/product-detail-nutrition.mapper.ts`
- `FoodDiary.Web.Client/src/app/features/meals/components/detail/meal-detail/meal-detail.ts`
- `FoodDiary.Web.Client/src/app/features/meals/components/detail/meal-detail/meal-detail.html`
- `FoodDiary.Web.Client/src/app/features/meals/components/detail/meal-detail-lib/meal-detail.mapper.ts`
- `FoodDiary.Web.Client/src/app/features/recipes/components/detail/recipe-detail/recipe-detail.ts`
- `FoodDiary.Web.Client/src/app/features/recipes/components/detail/recipe-detail/recipe-detail.html`
- `FoodDiary.Web.Client/src/app/features/recipes/components/detail/recipe-detail-lib/recipe-detail-nutrition.mapper.ts`
- `FoodDiary.Web.Client/src/app/features/products/components/manage/product-manage-lib/product-manage-form.types.ts`
- `FoodDiary.Web.Client/src/app/features/products/components/manage/product-manage-lib/product-manage-form.mapper.ts`
- `FoodDiary.Web.Client/src/app/features/products/components/manage/product-manage-form/product-manage-form.ts`
- `FoodDiary.Web.Client/src/app/features/products/components/manage/product-manage-form/product-manage-form.html`
- `FoodDiary.Web.Client/src/app/features/products/components/manage/product-basic-info/product-basic-info.ts`
- `FoodDiary.Web.Client/src/app/features/products/components/manage/product-basic-info/product-basic-info.html`
- `FoodDiary.Web.Client/src/app/features/products/components/manage/product-nutrition-editor/product-nutrition-editor.ts`
- `FoodDiary.Web.Client/src/app/features/products/components/manage/product-nutrition-editor/product-nutrition-editor.html`
- `FoodDiary.Web.Client/src/app/features/meals/components/manage/meal-manual-item-dialog/meal-manual-item-dialog.ts`
- `FoodDiary.Web.Client/src/app/features/meals/components/manage/meal-manual-item-dialog/meal-manual-item-dialog.html`
- `FoodDiary.Web.Client/src/app/features/meals/components/manage/meal-general-info/meal-general-info.ts`
- `FoodDiary.Web.Client/src/app/features/meals/components/manage/meal-general-info/meal-general-info.html`
- `FoodDiary.Web.Client/src/app/features/meals/components/manage/meal-nutrition-sidebar/meal-nutrition-sidebar.ts`
- `FoodDiary.Web.Client/src/app/features/meals/components/manage/meal-nutrition-sidebar/meal-nutrition-sidebar.html`
- `FoodDiary.Web.Client/src/app/features/recipes/components/manage/recipe-basic-info/recipe-basic-info.ts`
- `FoodDiary.Web.Client/src/app/features/recipes/components/manage/recipe-basic-info/recipe-basic-info.html`
- `FoodDiary.Web.Client/src/app/features/recipes/components/manage/recipe-nutrition-editor/recipe-nutrition-editor.ts`
- `FoodDiary.Web.Client/src/app/features/recipes/components/manage/recipe-nutrition-editor/recipe-nutrition-editor.html`
- `FoodDiary.Web.Client/projects/fd-ui-kit/src/lib/input/fd-ui-input.spec.ts`
- `FoodDiary.Web.Client/projects/fd-ui-kit/src/lib/textarea/fd-ui-textarea.spec.ts`

## Decoupled From Reactive Forms

- `FoodDiary.Web.Client/src/app/features/meals/components/manage/meal-items-section/meal-items-section.ts`
- `FoodDiary.Web.Client/src/app/features/meals/components/manage/meal-items-section/meal-items-section.spec.ts`
- `FoodDiary.Web.Client/src/app/features/meals/components/manage/meal-items-list/meal-items-list.ts`
- `FoodDiary.Web.Client/src/app/features/meals/components/manage/meal-items-list/meal-items-list.spec.ts`
- `FoodDiary.Web.Client/src/app/features/recipes/components/manage/recipe-steps-list/recipe-steps-list.ts`
- `FoodDiary.Web.Client/src/app/features/recipes/components/manage/recipe-steps-list/recipe-steps-list.html`
- `FoodDiary.Web.Client/src/app/features/recipes/components/manage/recipe-steps-list/recipe-steps-list.spec.ts`
- `FoodDiary.Web.Client/src/app/features/meals/components/manage/meal-manage-form.html`
- `FoodDiary.Web.Client/src/app/features/recipes/components/manage/recipe-manage/recipe-manage.html`
- `FoodDiary.Web.Client/src/app/shared/lib/validation-error.utils.spec.ts`
- `FoodDiary.Web.Client/src/app/features/meals/components/manage/meal-manage-lib/meal-manage-view.utils.spec.ts`
- `FoodDiary.Web.Client/src/app/features/recipes/components/manage/recipe-manage-lib/recipe-form-error.utils.spec.ts`

## Next Candidates

- Small dialogs/pages without `FormArray`.
- Forms backed by existing `fd-ui-*` CVA components when the submit/reset behavior is simple.

## Defer Until Patterns Are Proven

- Forms with `FormArray`.
- Forms using `setValidators`, `addValidators`, `removeValidators`, or `clearValidators`.
- Forms using imperative `enable()` / `disable()` until they can be mapped to declarative Signal Forms logic.
- Forms using `setErrors()` / `markAsPending()`.
- Complex `fd-ui-kit` ControlValueAccessor forms until UI-kit-specific patterns are proven.

## Migration Notes

- Use `signal()` for the form model and `form()` for the field tree.
- Import `FormField` instead of `ReactiveFormsModule` when a component only binds native controls through `[formField]`.
- `FormField` can interoperate with `ControlValueAccessor` components for backwards compatibility. Treat each `fd-ui-*` form migration as a small pilot until UI-kit-specific patterns are proven.
- Prefer declarative `disabled(path.field, { when: () => condition })` for disabled Signal Forms fields.
- `fd-ui-date-input` is adapted for Signal Forms CVA binding; keep internal control state away from public/protected `value` fields so Angular does not treat CVA components as Signal Forms custom controls accidentally.
- `fd-ui-date-range-input` is adapted for Signal Forms internally while keeping CVA compatibility for legacy consumers.
- `fd-ui-datetime-input` is adapted for Signal Forms internally while keeping CVA compatibility for legacy consumers.
- Product manage now keeps `ProductFormValues` as the signal model and passes mapped nutrition fields into the shared nutrition editor.
- Meal manual item dialog uses a scalar Signal Form for amount and now accepts/returns plain item values. `meal-manage-form` remains the legacy adapter for its items `FormArray`.
- Meal manage complex migration has started with value-based form factories, nutrition summary helpers, Signal Forms general-info and nutrition sidebar blocks. The root remains hybrid until items move off legacy forms.
- Meal manage root template no longer uses `[formGroup]`/`ngSubmit`; submit is handled through native `submit` while the TS adapter still owns the legacy item form array.
- Meal items list no longer needs a template `[formGroup]` and now renders value/error state from the parent adapter.
- Meal items section/list now render value/error state instead of receiving the legacy items `FormArray`; `meal-manage-form` remains the adapter until item editing moves to Signal Forms.
- Recipe manage complex migration has started with Signal Forms basic-info/nutrition fields. The root remains hybrid while step editing and step managers stay on legacy forms.
- Recipe manage root template no longer uses `[formGroup]`/`ngSubmit`; submit is handled through native `submit` while the TS adapter still owns legacy step editing.
- Recipe nutrition editor now consumes Signal Forms fields. `recipe-manage` remains the adapter for the legacy nutrition manager until the manager itself is migrated.
- Shared nutrition editor is now Signal Forms only; the legacy `FormGroup`/`controlNames` fallback was removed after all consumers moved to `[form]`.
- Recipe steps list now renders step-card state and emits reorder events instead of receiving the legacy steps `FormArray`; `recipe-manage` remains the adapter for step mutation.
- `fd-ui-input` and `fd-ui-textarea` host integration specs now use Signal Forms `[formField]`, including declarative disabled-state checks.
- Error utility specs now use structural control-state doubles instead of Angular `FormControl`, keeping actual Reactive Forms usage visible in adapters/managers.
- Keep custom array-style checkbox state explicit until a stable local pattern exists; `[formField]` does not cover multiple checkbox arrays directly.
- Update this file after each batch with migrated and remaining counts.
