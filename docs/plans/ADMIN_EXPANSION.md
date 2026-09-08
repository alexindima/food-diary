# Admin expansion

Accepted scope: the user requested implementation of the complete admin review on 2026-09-08. Existing mail and chart changes are the baseline and must be preserved. No production deployment or real outbound messages are part of local implementation.

## Acceptance checklist

- [x] Shared UTC calendar period controls, valid ranges, URL filters, browser back/forward and dashboard drill-down context.
- [x] Distinct loading/error/empty states, retry, localized navigation and mobile route parity.
- [x] AI daily/model/operation/user breakdowns, period filtering, explicit separation of observed usage from unavailable cost/latency/failure data.
- [x] Account registration/role/confirmation/activity filters and linked user detail route with related operational records.
- [x] Login and impersonation period/actor/provider/device filtering and account links.
- [x] Acquisition custom dates, comparison/trends and a paged event search.
- [x] Billing linked tabs/records and subscription/payment/webhook context, renewal/cancellation reporting with explicit definitions.
- [x] Incoming/outgoing mail date/search/detail links, correlation and actionable status counts.
- [x] Email template purpose/locale filtering, supported variables and revision history.
- [x] Lessons search/category/locale/preview, publication lifecycle and evidence-based completion analytics.
- [x] Achievement search/category/state and award statistics.
- [x] Moderation content context, actor/history/date/type filters and outstanding age.
- [x] User detail, AI prompts, read-only bug triage, scoped audit and retention routes.
- [x] Retention measures actual diary activity with explicit cohort maturity and denominator; no inference from login alone.
- [x] Focused behavioral tests, API snapshots, migrations where needed, architecture, frontend/browser and wiki verification.

## Implementation order

1. Shared navigation/filter/error foundation and existing AI data.
2. Account and operational list query contracts, details and cross-links.
3. Read-only reporting and integrations for bugs, audit, acquisition and retention.
4. Content/template/prompt editing, versioning and publication/statistics.
5. Consumer-aware verification and final acceptance audit.

Metrics without persisted evidence must be labelled unavailable until collection is implemented; historical data must never be fabricated. Service credentials remain server-side. New API adapters preserve module ownership and existing privilege checks.

## Delivered behavior and validation

Implemented in the working tree; no production deployment or real mail dispatch was performed. The new user detail, retention, audit, bug investigation and AI prompt routes are linked from the admin navigation. Incoming and outgoing messages remain under the Mail section. Details and operational lists carry record and user identifiers through URLs.

Verification includes the complete backend solution build, 1,202 architecture tests, 197 Admin application tests, 321 presentation policy tests, 44 Admin presentation tests, and three regenerated then verified Swagger snapshots. Focused PostgreSQL suites cover owner template revision persistence and restoration, lesson publication, moderation context and filtering, billing ID searches, retention cohorts, acquisition ranges, user/AI scoping, and mail pagination/status counts. Frontend verification covers builds, lint and dependency/state/component guards, design tokens, translations, and application/UI-kit/tour/admin tests. Browser smoke checks and local mocked desktop/mobile flows cover navigation, UTC dates, filter history, revision restoration and unsaved changes. Browser mocks are not evidence of a live production integration.

The reporting definitions and release conditions are documented in `docs/backend/ADMIN_REPORTING.md`. The bug journal needs a separate server-side read credential. Historical template revisions begin with subsequent edits. Audit coverage is limited to persisted collaboration events. Billing cancellation counts describe current scheduled state. No unavailable AI financial or latency metrics are fabricated.
