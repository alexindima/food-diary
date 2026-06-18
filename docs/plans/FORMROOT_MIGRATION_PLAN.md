# FormRoot Migration Plan

## Context

Product creation exposed a native browser submit regression: a Signal Forms page used a plain `<form>` submit path, and the browser navigated with `ng.form1.*` query parameters instead of running the save flow.

Angular Signal Forms provide `FormRoot` and `submit()` for this exact lifecycle. `FormRoot` should own native form submit behavior, prevent browser navigation, set `novalidate`, and trigger the configured Signal Forms submission action.

## Goals

- Move Signal Forms-backed submit forms to `FormRoot`.
- Keep business submit logic in existing components/facades where possible.
- Add regression tests that native `submit` is cancelled and the intended save/apply handler runs.
- Avoid changing non-submit forms, pure filter/search forms, or Reactive Forms unless they have the same native submit risk.
- Classify all Signal Forms submit surfaces before migrating so child forms do not accidentally take root ownership.

## Non-Goals

- Do not redesign forms or validation UI during this migration.
- Do not convert Reactive Forms to Signal Forms.
- Do not move all submit business logic into `form(..., { submission })` in one pass if that increases risk.

## Candidate Groups

### Priority 1: Entity Manage Pages

These are user-facing create/edit flows with the highest impact:

- `src/app/features/products/components/manage/product-manage-form` - migrated.
- `src/app/features/recipes/components/manage/recipe-manage` - migrated.
- `src/app/features/meals/components/manage/meal-manage-form` - migrated.

Target pattern:

```html
<form [formRoot]="entityForm"></form>
```

```ts
private readonly submitEntityFormAsync = async (): Promise<void> => {
    await this.onSubmitAsync();
};

protected readonly entityForm = form(
    this.entityFormModel,
    path => {
        // validators
    },
    {
        submission: {
            action: this.submitEntityFormAsync,
        },
    },
);
```

For synchronous submit methods, either make the method genuinely async by awaiting the underlying facade call, or keep the existing manual submit until the facade API can be made promise-returning cleanly.

If the form has validation outside the root `FieldTree` (for example step/item collection validation or macro consistency checks), keep those guards in `submission.action` and add `submission.onInvalid` for root-field invalid submits. `FormRoot` handles native submit cancellation and root touch state, but local collection touched state still belongs to the component/facade that owns it.

### Priority 2: Standalone Signal Forms With Submit Buttons

Audit and migrate if they are real submit forms:

- `src/app/features/auth/pages/password-reset` - migrated.
- `src/app/features/profile/pages/user-manage` - migrated.
- `src/app/features/profile/dialogs/change-password-dialog` - migrated.
- `src/app/features/goals/dialogs/calorie-goal-dialog` - migrated.
- `projects/fooddiary-admin/src/app/features/admin-users/dialogs/admin-user-impersonation-dialog` - migrated.
- `projects/fooddiary-admin/src/app/features/admin-email-templates/dialogs/admin-email-template-edit-dialog` - migrated.

Some of these already use `(submit)="...; $event.preventDefault()"`. Migrate only when the component owns a `FieldTree` root and can define `submission.action` without making the flow less clear.

### Additional Root-Owned Signal Forms To Audit

These were found outside the initial list and should be classified in the next pass:

- `src/app/features/dietologist/pages/client-dashboard` - migrated date filter and recommendation root forms.
- `src/app/features/cycle-tracking/pages/cycle-tracking-page` - migrated facade-owned start/day/factor root forms.
- `src/app/features/shopping-lists/pages/shopping-list-items-panel` - migrated as a child DOM root using the parent-owned configured `FieldTree`.
- `src/app/features/weight-history/components/weight-history-form-card` - migrated as a child DOM root using the facade-owned configured `FieldTree`.
- `src/app/features/waist-history/components/waist-history-form-card` - migrated as a child DOM root using the facade-owned configured `FieldTree`.
- `src/app/features/explore/dialogs/report-dialog` - Signal Forms dialog without native `<form>` submit today; no immediate native submit risk.
- `src/app/features/explore/components/comments/recipe-comments` - Signal Forms comment flow without native `<form>` submit today; no immediate native submit risk.
- `src/app/features/meals/pages/list/meal-list-filters-dialog` - filter/apply form; likely exclusion unless native navigation is possible.
- `projects/fooddiary-admin/src/app/features/admin-lessons/dialogs/admin-lesson-edit-dialog` - Signal Forms without native `<form>` submit today; no immediate native submit risk.
- `projects/fooddiary-admin/src/app/features/admin-users/dialogs/admin-user-edit-dialog` - Signal Forms dialog without native `<form>` submit today; no immediate native submit risk.
- Auth child forms under `src/app/features/auth/components/auth/*-form` remain pending; root ownership lives in `AuthFormManager`, while submit orchestration lives in `AuthComponent`.

### Priority 3: Child Form Components

These components often receive a `FieldTree` from a parent and emit `formSubmit`:

- `src/app/features/weight-history/components/weight-history-form-card`
- `src/app/features/waist-history/components/waist-history-form-card`
- Auth child forms under `src/app/features/auth/components/auth/*-form`

Do not blindly add `FormRoot` here. Decide whether the parent should own the root submission, or whether the child should receive a configured root `FieldTree` and use `FormRoot`.

Weight and waist history now use the second pattern: their facades configure `submission.action`, and the child form cards own only the DOM `FormRoot`.

### Likely Exclusions

- Search/filter forms where native submit is intentionally used only to apply local filters.
- Forms not backed by Signal Forms.
- UI kit custom controls that implement `FormValueControl` but are not form roots.

## Implementation Steps

1. Audit all `@angular/forms/signals` imports and all `<form>` templates.
2. Classify each form as root submit form, child submit form, search/filter form, or non-Signal form.
3. Migrate Priority 1 first.
4. Add tests per migrated form:
    - dispatch a cancelable `submit` event;
    - assert `dispatchEvent(...)` returns `false`;
    - assert `event.defaultPrevented === true`;
    - assert the existing save/apply facade method was called.
5. Run focused tests for migrated components.
6. Run `npm run lint`, `npm run stylelint`, and relevant app/admin tests.
7. Commit migration separately from unrelated UI changes.

## Risks

- `FormRoot` action callbacks are async-oriented. Existing synchronous submit methods may need a small facade contract cleanup to avoid lint exceptions.
- Child components that only emit submit events may need a parent-owned migration rather than local `FormRoot`.
- `FormRoot` marks fields touched and handles validation gating, so manual submit logic should be reviewed for duplicate or missing validation behavior.

## Current Finding

Product creation failed in browser before migration because native form submission was not prevented. The URL became `/products/add?ng.form1.caloriesPerBase=...`, and no product was created.

## Migration Status

Completed in the first migration pass:

- Product manage form was already migrated and has a native submit regression test.
- Meal manage form now uses `FormRoot`, `submission.action`, `submission.onInvalid`, and a native submit regression test.
- Recipe manage form now uses `FormRoot`, `submission.action`, `submission.onInvalid`, and a native submit regression test.
- Password reset, change password, calorie goal, user profile manage, admin impersonation, and admin email template edit forms now use `FormRoot` with native submit regression tests.
- Dietologist client dashboard date/recommendation forms, cycle tracking facade forms, and shopping list quick-add form were migrated in the second pass.
- Weight and waist history entry cards were migrated in the third pass with facade-owned submission actions and child-card native submit regression tests.

Verification run:

- `cd FoodDiary.Web.Client && npm run test:ci:app -- --include ...` for migrated app specs.
- `cd FoodDiary.Web.Client && npm run test:ci:admin -- --include ...` for migrated admin specs.
