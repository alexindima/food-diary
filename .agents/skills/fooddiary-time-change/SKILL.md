---
name: fooddiary-time-change
description: Implement or review FoodDiary behavior involving calendar dates, meal timestamps, day/week/month filters, timezone conversion, or period boundaries. Preserve the distinct date contracts across UI and API; do not apply to unrelated Angular styling or generic scheduling advice.
---

# FoodDiary time change

Classify the temporal meaning of a value before converting it. Resolve repository references below against the current checkout and follow its applicable guides and change route.

## Find the owning contract

Trace user input, URL state, API payload/query, storage representation, response mapping, and rendered date. Identify which contract applies:

- Calendar date: measurement and similar day labels must preserve the selected day. Their current API encoding may use UTC midnight without making them a user's local midnight instant.
- Instant: a meal's date and time must survive local input -> API instant -> edit without shifting the user's selected local date/time.
- Reporting range: determine whether each endpoint accepts inclusive calendar dates or an exclusive upper instant. Admin UTC reports and user local-day reports need their own boundaries.
- Relative selection: a period such as last seven days remains a relative preset where the existing URL/state contract says so; materializing its boundaries for a request need not replace the preset in navigation state.

Read current sources as needed:

- Calendar helpers in `FoodDiary.Web.Client/src/app/shared/lib/`: `measurement-date.utils.ts`, `local-date.utils.ts`, and `goal-history-date.utils.ts`.
- `FoodDiary.Web.Client/src/app/features/dashboard/lib/dashboard-date.utils.ts`.
- `FoodDiary.Web.Client/src/app/features/meals/components/manage/meal-manage-lib/meal-manage-form.mapper.ts`.
- `FoodDiary.Web.Client/projects/fooddiary-admin/src/app/shared/period/admin-period.ts`.
- `docs/backend/ADMIN_DASHBOARD.md` and the affected endpoint's validators/contracts.

Do not make all of these values UTC or local merely for consistency. Preserve their distinct user-visible meaning.

## Check the relevant edges

Use the affected flow's existing calendar tests and concrete outcomes:

- Input and edit round trip near midnight, month/year boundaries, leap day, and invalid calendar dates.
- Local zones on either side of UTC and non-whole-hour offsets; include daylight-saving transitions where the flow uses local instants.
- Range inclusion at the first and last boundary and exclusion of the next range's first instant.
- Day/week/month navigation, browser back/forward, reload, and stale responses after switching the selected period.
- Preserve the current policy for invalid, reversed, future, or excessive report ranges.

Use an injected clock or existing pure-helper `now` parameter for date-dependent tests rather than changing the workstation clock. Derive calendar boundaries with the owning date semantics; do not assume every local day is a fixed 24-hour interval.

## Reuse executable evidence

The maintained `FoodDiary.Web.Client/scripts/test-measurement-timezones.mjs` defines the zone matrix for measurement, dashboard, and meal calendar suites. Use the relevant focused tests while iterating and the maintained runner when validating changes across those flows; report which zones and suites actually ran.

Relevant starting points include `measurement-calendar-timezone.spec.ts`, `measurement-history-pager.spec.ts`, `dashboard-calendar-timezone.spec.ts`, and `meal-calendar-timezone.spec.ts`. Verify their current paths and coverage before using a passing result to support a different period or endpoint.

In browser checks, select a different period, wait for its response and rendered selection, then verify that old data is not presented as the new period. A successful GET alone does not prove the selected period is the one displayed. A missing optional URL parameter is only a defect when the current feature contract requires it.

Report the temporal contract preserved, boundary cases and timezone evidence, and affected flows still requiring verification. Keep enduring date policy in owning code/tests/docs; this skill routes and verifies changes rather than introducing a second date specification.
