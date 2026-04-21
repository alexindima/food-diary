# Accessibility Checklist

This checklist is the baseline manual audit for the Food Diary frontend.
Use it when reviewing new features and when doing release-polish passes.
Use [ACCESSIBILITY_AUDIT_TEMPLATE.md](C:\Users\alexi\OneDrive\Документы\GitHub\food-diary\FoodDiary.Web.Client\ACCESSIBILITY_AUDIT_TEMPLATE.md) to record screen-by-screen results.

Status meanings:
- `[x]` covered and verified in code or recent audit
- `[~]` partially covered, needs manual verification
- `[ ]` missing or not reviewed yet

## Core Checks

### Keyboard Flow
- `[x]` Primary navigation is reachable with keyboard
- `[x]` Lazy-route navigation gives immediate visible feedback
- `[x]` Dialogs focus the first tabbable element on open
- `[x]` Dialogs close on `Escape`
- `[x]` Menus close on `Escape`
- `[x]` Menus close on `Tab` without trapping focus unexpectedly
- `[x]` Sidebar overlays restore focus to the trigger on close
- `[~]` Custom drawers and secondary overlays restore focus consistently

### Focus Visibility
- `[x]` Buttons show visible `:focus-visible`
- `[x]` Dialog close button shows visible `:focus-visible`
- `[x]` Sidebar links and section headers show visible `:focus-visible`
- `[x]` Mobile nav items show visible `:focus-visible`
- `[~]` Remaining page-specific custom controls show visible `:focus-visible`

### Accessible Names
- `[x]` Icon-only buttons in shared kit accept `ariaLabel`
- `[x]` Sidebar/user-menu actions have explicit names
- `[x]` Quick consumption drawer actions have explicit names
- `[x]` AI photo result destructive and dismiss actions have explicit names
- `[x]` Image upload controls have explicit names
- `[~]` Long-tail icon-only actions across rarely used screens still need manual review

### Live Regions And Status
- `[x]` Global loading is visible for long requests and route chunk loading
- `[x]` Button loading is visible on key mutation actions
- `[x]` Shared `fd-ui-form-error` now exposes alert semantics
- `[x]` Notifications dialog loading and empty states use `role="status"`
- `[x]` AI analysis and upload overlays expose status updates
- `[x]` Form submission errors use alert semantics in key forms/dialogs
- `[~]` Toasts and success feedback still need a manual screen-reader pass

### Forms
- `[x]` Auth flows expose busy states while submitting
- `[x]` Change password dialog exposes field and submit errors as alerts
- `[x]` Meal filters dialog has a proper form submit flow
- `[~]` Less common settings and manage forms need a final manual tab/order pass

### Empty, Error, And Section States
- `[x]` Shared `fd-ui-empty-state` exists
- `[x]` Shared `fd-ui-section-state` exists
- `[x]` Goals page uses explicit loading/error state patterns
- `[x]` Statistics page uses page and section state patterns
- `[~]` Remaining secondary screens need consistency verification

### Theme And Contrast
- `[~]` Light themes look consistent after recent focus-ring changes
- `[ ]` Dark theme contrast audit across key screens
- `[ ]` High-density mobile audit for bottom nav, sheets, and dialogs

## Screen Baseline

### Auth
- `[x]` Login/register/reset forms expose busy states
- `[x]` Google unavailable hint is announced as status
- `[~]` Manual keyboard pass on embedded auth dialog variant

### Dashboard
- `[x]` Uses section loading/defer patterns
- `[~]` Manual screen-reader pass still needed for widget sequence and action labels

### Meals
- `[x]` List page has loading/error/empty patterns
- `[x]` Filters dialog follows form semantics
- `[x]` Quick consumption drawer has better labels and live feedback
- `[~]` Meal add/edit/detail still need final manual audit for tab order and destructive actions

### Products
- `[x]` Detail dialog favorite action has explicit label
- `[~]` Product add/edit/detail need manual review for icon-only and destructive flows

### Recipes
- `[x]` List/manage areas use improved loading feedback
- `[~]` Comments/report/detail flows need a final manual a11y pass

### Goals
- `[x]` Page states normalized
- `[~]` Manual keyboard and screen-reader verification still pending

### Statistics
- `[x]` Page and section states normalized
- `[~]` Chart accessibility still needs manual verification

### Fasting
- `[x]` Key async actions use button loading
- `[x]` Control groups now use localized accessible names
- `[x]` Expanded history check-in regions are linked to their toggles
- `[~]` Complex action matrix still needs end-to-end manual audit

### Profile
- `[x]` Settings feedback is clearer
- `[x]` Connected devices section uses shared section-state pattern
- `[x]` Main settings surface exposes a combined busy state
- `[~]` Permissions and device-management flows still need final keyboard review

### Sidebar And Notifications
- `[x]` Sidebar overlays support `Escape`, focus handoff, and restore focus
- `[x]` Notifications dialog exposes status and list semantics
- `[~]` Manual mobile sheet audit still pending on real device sizes

### Image Upload
- `[x]` Upload zone exposes busy/error semantics
- `[x]` Cropper overlay exposes dialog semantics
- `[~]` Manual audit still needed for cropper keyboard handling expectations

## Priority Backlog

### P0
- `[ ]` Run a manual dark-theme contrast audit on key screens
- `[ ]` Run a keyboard-only pass on `meal manage`, `product manage`, `recipe manage`, `fasting`, and `profile`

### P1
- `[ ]` Audit chart accessibility in `statistics` and confirm fallback comprehension without chart visuals
- `[ ]` Audit toast announcements and confirm they are not the only success/error signal
- `[ ]` Review long-tail icon-only controls in secondary screens: `shopping-lists`, `meal-plans`, `weekly-check-in`, `gamification`, `lessons`, `dietologist`

### P2
- `[ ]` Add canonical screen-pattern docs to Storybook: `List Page`, `Form Page`, `Settings Page`, `Detail Dialog`
- `[ ]` Convert this checklist into a repeatable release checklist with owner/date columns

## Suggested Audit Order

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

## Notes

- Prefer fixing repeated issues in `fd-ui-kit` before patching app screens one by one.
- Do not rely on toast alone for critical outcomes.
- Do not treat code-level improvements as a substitute for a real keyboard and screen-reader pass.
