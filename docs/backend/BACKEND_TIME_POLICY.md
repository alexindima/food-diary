# Backend Time Policy

Date: 2026-03-28

## Goal

Keep backend time-dependent behavior deterministic, testable, and consistent across layers.

## Rules By Layer

### Application

- Use .NET `TimeProvider` for current UTC time.
- Do not call `DateTime.UtcNow` directly in handlers, services, or validators.

### Presentation

- Do not call `DateTime.UtcNow` directly in controllers or HTTP mappings.
- Inject `TimeProvider` when presentation code needs current UTC time.
- Keep controllers and HTTP mappings deterministic in tests by using a fixed `TimeProvider`.

### Infrastructure

- Use `TimeProvider` for operational timestamps, expirations, quota windows, and repository-side defaults.
- Direct `DateTime.UtcNow` is allowed only in generated EF migration snapshots or other generated artifacts.

### Domain

- Domain time policy is intentionally stricter and needs explicit design decisions.
- Until the domain refactor pass, avoid broad churn.
- New domain code should prefer receiving normalized timestamps from callers when practical.
- Existing direct clock usage in domain is tracked separately and should be reduced deliberately, not opportunistically.

## Approved Exceptions

- Generated files may contain fixed or generated UTC timestamps.
- Tests may use `DateTime.UtcNow` when they are explicitly testing relative current-time behavior, but fixed `TimeProvider` implementations are preferred for deterministic behavior.

## Immediate Migration Order

1. Presentation mappings and controllers
2. Infrastructure services and repositories
3. Application leftovers
4. Domain audit fields and events in a dedicated pass

## Body measurement calendar dates

Weight and waist measurements belong to the date selected in the client, not the UTC date of the submission instant. Existing storage and response values encode that calendar date as UTC midnight; no historical records are rewritten. Clients must preserve the date when editing and display it without a local-time conversion. The web date picker uses the device calendar to choose today's date.

`GET /statistics/summary` accepts optional paired `bodyDateFrom` and `bodyDateTo` (`YYYY-MM-DD`) for inclusive measurement bounds. They are validated for completeness, ordering, and the existing maximum period. Nutrition continues to use the `dateFrom`/`dateTo` instants. Requests without the new pair retain the legacy UTC-date interpretation for compatibility; old clients should upgrade to use explicit calendar bounds. This does not change profile time-zone settings or meal timestamp semantics.

Run `npm run test:ci:measurement-timezones` in `FoodDiary.Web.Client` for the frontend matrix (UTC, Tbilisi, Los Angeles, St. John's, Kathmandu, Kiritimati, Pago Pago, Berlin, Lord Howe). Statistics application tests cover the same zones, DST transitions, and invalid ranges. `MeasurementCalendarDateIntegrationTests` verifies persisted adjacent-day weight/waist entries through HTTP and PostgreSQL.

## Dashboard local calendar

The web dashboard sends the selected calendar date (encoded at UTC midnight) and the device IANA `timeZoneId`. The backend resolves each local midnight independently, including the next midnight and all seven nutrition buckets. Never add a fixed 24 hours to derive the next local day. Ambiguous midnight uses its earliest occurrence; an invalid midnight advances to the first valid local time. A nonexistent selected calendar day is rejected. Invalid zone IDs return validation errors rather than silently using UTC.

`timeZoneOffsetMinutes` remains a legacy fixed-offset fallback when `timeZoneId` is absent; no zone and no offset retain UTC behavior. The named zone takes precedence. No persistent time-zone or measurement migration is required. Existing profile time-zone settings are not overwritten.

`DashboardCalendarRange` carries calendar bounds separately from UTC event bounds. Weight/waist latest and trend reads, exercise totals, daily advice and cycle and TDEE calculations use calendar dates. TDEE daily calorie keys use the same local calendar as its weight/exercise samples. Meals and hydration use the actual event interval. Nutrition bucketing preserves local days and calendar-day divisors across DST with one weekly read. Hydration fallback reads opt into exact bounds instead of expanding to UTC dates. Today's quick water entry uses the current instant; historical quick entries use local noon.

Regression checks: `DashboardCalendarTests`, `LocalCalendarTests`, `DashboardTimeZoneIntegrationTests` (HTTP and PostgreSQL), and the frontend `test:ci:measurement-timezones` matrix now covering dashboard helpers as well as measurement histories. The user-scoped output cache varies by all query parameters, including `timeZoneId`.

## Nutrition and hydration goal history

Users owns `UserGoalRevision` and `UserGoalHistory`. A changed calorie, nutrient, water,
hydration, step or calorie-cycling setting takes effect for the entire current calendar
day in the saved user time zone (`UTC` when unset). `RecordedAtUtc` records the instant;
`EffectiveFrom` is a calendar date and is never shifted when reporting in another zone.
Multiple changes on one day resolve by the monotonically increasing profile revision.
Changing the profile time zone does not rewrite existing effective dates.

Current values remain on `UserNutritionProfile`. Its concurrency version and goal
revision counter coordinate the profile and append-only history in one transaction.
Snapshot identities are independent of the counter so competing saves reach the
existing profile concurrency check; a failed save rolls back its history too. Tracked
profile reads do not load historical revisions. Dated projections read one baseline
plus the last revision of each day in a bounded range, without tracking entities.

Dashboards resolve daily goals by calendar date. The seven-day calorie target sums
the seven days ending on the selected day; a multi-day report sums its own period.
Nutrient targets use the period's daily average, matching consumption averages, and
water targets sum the daily values. A period containing an unset nutrient/water goal
keeps that target null. Dated hydration totals and diary calorie summaries also use
the historical values. Gamification compares each recent day's calories to that day's
target; weekly check-in suggestions use the selected week's average target, including
calorie cycling. Weight and waist goals keep their existing separate histories.

`AddUserGoalHistory` seeds existing profiles with their actual current values, including
nulls, as a baseline from `DateOnly.MinValue` (PostgreSQL `-infinity`). Previously
overwritten values cannot be recovered. New accounts retain existing registration
defaults. Apply the migration with stopped writers and the matching application;
old writers cannot record history. Down removes history and its counter but leaves
the latest profile values intact. Prefer roll-forward once history has been collected.
