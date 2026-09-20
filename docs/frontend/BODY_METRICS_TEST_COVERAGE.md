# Weight and waist frontend test coverage

Audit date: 2026-09-20. Baseline: commit `98ec62da6`.

## Scope and measurement

The audit covers every production TypeScript file under `src/app/features/weight-history/` and `src/app/features/waist-history/`: page composition, feature facades, API adapters, cards, dialogs, routes, configuration, tours, and pure mappers/utilities. Both feature trees are explicitly included in V8 coverage, including files that were not previously imported by tests. Production files were not excluded to raise percentages. Type-only files have no executable statements.

The baseline contained 19 spec files and 104 tests. It covered 56.89% of lines, 59.08% of statements, 61.42% of functions, and 56.79% of branches. HTTP service branch coverage was already 100%, even though service line coverage was only 66.66% (weight) / 68.18% (waist): unexecuted error callbacks demonstrate why one percentage alone is misleading.

The new tests exercise standalone components and Angular Signal Forms. Page tests use their real component-scoped facade, real child templates, and controlled API/dialog transports. Deferred content is explicitly rendered with Angular's test API; tests do not replace the page template or require a fake browser viewport observer.

## Coverage results

The final focused run passes **312 tests in 33 files** (208 additional cases). Aggregate coverage:

| Metric     | Before | After              |
| ---------- | ------ | ------------------ |
| Lines      | 56.89% | 99.32% (1031/1038) |
| Statements | 59.08% | 99.40% (1169/1176) |
| Functions  | 61.42% | 100% (294/294)     |
| Branches   | 56.79% | 98.16% (642/654)   |

| Layer            | Lines before | Lines after | Branches before | Branches after |
| ---------------- | ------------ | ----------- | --------------- | -------------- |
| Weight API       | 66.66%       | 100%        | 100%            | 100%           |
| Weight Facade    | 88.23%       | 98.53%      | 65.68%          | 96.15%         |
| Weight Page      | 0%           | 100%        | 0%              | 100%           |
| Weight Goal card | 0%           | 100%        | 0%              | 100%           |
| Waist API        | 68.18%       | 100%        | 100%            | 100%           |
| Waist Facade     | 84.39%       | 98.54%      | 62.5%           | 96.22%         |
| Waist Page       | 0%           | 100%        | 0%              | 100%           |
| Waist Goal card  | 0%           | 100%        | 0%              | 100%           |

All eight dialogs now have specs; previously none did. All API adapter functions and statements are covered. Counts are V8-instrumented TypeScript/source-map counts; importing previously untested Angular components can change the denominator, so the behavior matrix remains the primary interpretation of these numbers.

## What is protected

| Layer               | Behaviors exercised                                                                                                                                                                                                                                                                                                          |
| ------------------- | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| API adapters        | HTTP methods, paths, filters, summary quantization and entry limits; successful payloads; read fallbacks; propagation of create/update/delete/page-summary errors                                                                                                                                                            |
| Facades             | Initialization, empty responses, latest measurement independent of chart period, loading/saving lifecycle, create/update/delete, invalid dates and bounds, duplicate and unknown save errors, retained edit input on failure, successful-save versions, cancellation, unit conversion, destruction of a pending subscription |
| Period state        | Preset changes, retained/reset custom selection, incomplete date ranges, repeated-range deduplication, chart/monthly KPI synchronization, refresh after a mutation outside the month view                                                                                                                                    |
| Signal Forms        | Configured submission action, localized decimal input, invalid input rejection, canonical API payloads                                                                                                                                                                                                                       |
| Goal lifecycle      | Numeric/blank/invalid goals, cancellation acknowledgement, active versus completed history, target updates and dialog save versions                                                                                                                                                                                          |
| Goal cards          | Loss/gain goals, movement away from goal, overshoot, zero-distance goals, clamped progress, signed changes, absent data, imperial rendering, invalid/reversed dates, estimates, incomplete completed-goal data                                                                                                               |
| Chart cards/mappers | Loading/empty/populated states, latest-measurement shortcut, null gaps, absent goal line, sorting, localized date labels, year changes, malformed date fallback                                                                                                                                                              |
| Entry cards/dialogs | Empty/loading states, 0/1/5/6-record boundaries, adjacent-record differences, today labels, real edit/delete/show-all button clicks and emitted payloads                                                                                                                                                                     |
| Edit/goal dialogs   | Old versus new save versions, pending saves, close after success, destruction of effects, retained server errors, edit cancellation, goal action buttons                                                                                                                                                                     |
| Pages               | Real facade scope, desktop/mobile header action, both lower cards, shared facade passed into dialogs, all-records dialog results, last-measurement period selection, monthly KPI tones, absent goal/current/start values, navigation and tour invocation                                                                     |
| Pure utilities      | Existing range, BMI, waist-to-height and goal-direction cases retained; invalid and cross-year chart date cases expanded                                                                                                                                                                                                     |

## Defect found by the new tests

Both entry facades validated comma decimals with `parseDecimalInput` but constructed payloads with `Number`. Thus `75,5` passed validation and produced `NaN` for the API. Regression tests failed against the original code. Payload construction now uses the same decimal parser and rejects a failed parse before canonical unit conversion. Tests cover public submission and the configured Signal Forms action for both measurements.

## Remaining limits and audit findings

High statement coverage is not exhaustive scenario coverage. In particular:

- Browser layout, chart rendering, focus restoration, keyboard operation and screen-reader behavior are not established by these jsdom component tests. A browser journey is still needed for those properties.
- Frontend/API tests mock transport; they do not prove database persistence, authorization, uniqueness constraints, or end-to-end refresh behavior against a running backend.
- The existing `test:ci:measurement-timezones` command separately runs calendar regressions in nine time zones. Feature coverage is measured in the current process zone; it should not be presented as nine-zone coverage of every dialog or page.
- Source review found that overlapping range requests have no cancellation or response-version check in the facades. Out-of-order responses and a page-summary response arriving after a period switch need dedicated concurrency regression work.
- Page-summary, delete and goal mutation subscriptions have no local error callback. Their adapters rethrow errors, while summary/history reads use empty fallbacks. The API tests verify these contracts; a complete page-level error/retry experience remains separate work. A failed fetch can otherwise resemble genuinely missing data.
- Repeated entry submission while a request is pending has no facade-level in-flight guard. UI disabling reduces exposure but does not establish request idempotency.
- A few defensive branches are unreachable through the current valid form path (empty payload checks after form validation, a goal form's invalid branch when it has no validators, and string split fallbacks). Tests do not call private methods or falsify validators merely to obtain 100%.

These findings are recorded explicitly rather than hidden behind the coverage percentages. Runtime changes in this audit are limited to the confirmed decimal payload defect.

## Verification performed

- Focused suite and configured thresholds: 312 tests / 33 files passed on the final runtime code.
- Full app regression: 3130 tests / 514 files passed. The final ten additional entry-card boundary cases were then checked in the focused suite.
- `npm run build`, scoped ESLint, strict dependency lint and Prettier checks passed.
- The nine-zone calendar command and real-browser/backend journeys were not rerun in this audit; they are not included in the verification claims above.

## Reproduction and regression gate

From `FoodDiary.Web.Client`:

```sh
npm run test:coverage:body-metrics
npm run test:ci:app
npm run build
npm run test:ci:measurement-timezones
```

The focused coverage command writes HTML, JSON and LCOV to `coverage/food-diary-web-client/`. Vitest thresholds require, for each feature tree, at least 99% lines/statements, 100% functions and 97% branches. The thresholds also apply when the regular app coverage job includes these files. The existing `.github/workflows/ci-tests.yml` app job runs `test:coverage:app`, so the thresholds participate in CI. Ordinary `test:ci:app` runs the specs but does not collect coverage.

Test helpers remain feature-local. No test uses `any`, disables hooks, or weakens lint/coverage rules.
