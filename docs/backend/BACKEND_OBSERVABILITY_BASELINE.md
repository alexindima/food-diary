# Backend Observability Baseline

Date: 2026-03-28
Scope: backend transport, business-flow, and operational telemetry

## Purpose

Define the first observability baseline for the backend.

## Existing Signals

The backend already had:

- request count and request duration
- unhandled request exception count
- rate-limit rejection count
- presentation operation counters and durations
- OpenTelemetry export wiring

## First Added Business-Flow Signals

The first observability expansion adds a dedicated counter for high-value business routes at the HTTP host boundary.

Metric:

- `fooddiary.api.business_flow.events`

Current tagged flows:

- `auth.register`
- `auth.login`
- `auth.refresh`
- `auth.restore`
- `auth.password-reset.request`
- `auth.password-reset.confirm`
- `auth.verify-email`
- `auth.verify-email.resend`
- `images.upload-url`
- `images.delete`
- `users.delete`

Current outcome tags:

- `success`
- `client_error`
- `server_error`

## Added Background Job Signals

The observability baseline now also includes stable metrics for recurring cleanup jobs in `FoodDiary.JobManager`.

Metrics:

- `fooddiary.job.execution.events`
- `fooddiary.job.deleted_items`
- `fooddiary.job.execution.duration`
- `fooddiary.job.last_success_age`
- `fooddiary.job.failure_streak`

Current tagged jobs:

- `images.cleanup`
- `users.cleanup`

Current outcome tags:

- `success`
- `failure`

State dimensions:

- `fooddiary.job.name`

## Added AI Provider Signals

The backend now also exposes operational metrics at the AI provider boundary in `FoodDiary.Infrastructure`.

Metrics:

- `fooddiary.ai.requests`
- `fooddiary.ai.quota_rejections`
- `fooddiary.ai.fallbacks`

Current tagged dimensions:

- operation:
  - `vision`
  - `nutrition`
- request outcome:
  - `success`
  - `transport_error`
  - `timeout`
  - `invalid_json`
  - `http_<status>`
  - `retry_exhausted`

Current diagnostic value:

- quota exhaustion becomes visible without inferring it from HTTP 429 responses alone
- fallback activation becomes measurable when the primary vision model degrades
- provider-side failure modes become separable from parsing failures

## Added External Provider Signals

The backend also exposes catalog-provider metrics at the integrations boundary.

Metrics:

- `fooddiary.external_provider.requests`
- `fooddiary.external_provider.duration`

Current tagged dimensions:

- provider:
  - `open_food_facts`
- operation:
  - `barcode_lookup`
  - `search`
- request outcome:
  - `success`
  - `not_found`
  - `empty`
  - `stale_cache`
  - `timeout`
  - `canceled`
  - `transport_error`
  - `http_error`
  - `json_error`

Current diagnostic value:

- Open Food Facts degradation is visible without mixing expected third-party timeouts into backend problem logs
- fresh in-memory cache hits do not count as external provider requests
- stale-cache fallback is visible as graceful degradation, not a backend incident

## Added Output Cache Signals

The HTTP host now also emits output-cache observations for cache-enabled presentation routes.

Metric:

- `fooddiary.api.output_cache.events`

Current tagged dimensions:

- policy:
  - `PresentationAdminAiUsageCache`
  - `PresentationUserScopedCache`
- outcome:
  - `hit`
  - `miss`

Current diagnostic value:

- user-scoped and admin cache effectiveness is visible without inferring it from latency alone
- cache regressions after route, vary-by, or auth changes become easier to spot

## Added Database Failure Signals

The infrastructure layer now also emits a stable counter when EF Core database commands fail.

Metric:

- `fooddiary.db.command.failures`

Current tagged dimensions:

- operation:
  - `reader`
  - `scalar`
  - `non_query`
- source:
  - EF Core command source name such as `LinqQuery` or `SaveChanges`
- error type:
  - `timeout`
  - `canceled`
  - provider exception type such as `PostgresException`

Current diagnostic value:

- PostgreSQL failures become visible without inferring them only from route-level 5xx volume
- read-path, write-path, and migration-related query failures become easier to separate
- repeated provider exceptions can be correlated quickly with deploy, schema, or infrastructure changes

## Added Email Delivery Signals

The infrastructure layer now also emits a stable counter for SMTP-backed auth email delivery.

Metric:

- `fooddiary.email.dispatch.events`

Current tagged dimensions:

- template:
  - `email_verification`
  - `password_reset`
- locale:
  - `en`
  - `ru`
- outcome:
  - `success`
  - `failure`
- error type:
  - exception type when delivery fails

Current diagnostic value:

- auth recovery incidents become easier to separate into token-generation success versus mail-delivery failure
- localized template or SMTP failures become measurable without relying only on logs
- registration, verification resend, and password reset now have an observable dependency boundary

## Added Storage Boundary Signals

The infrastructure layer now also emits storage-operation telemetry for S3-compatible image handling.

Metric:

- `fooddiary.storage.operations`

Current tagged dimensions:

- operation:
  - `presign`
  - `delete`
- outcome:
  - `success`
  - `failure`
  - `validation_error`
- error type:
  - exception type when an operation fails

Current diagnostic value:

- upload-link generation failures become separable from downstream client upload issues
- delete failures during cleanup or explicit asset removal are visible without relying only on logs
- validation/config errors and provider failures can be distinguished on the storage boundary

## Why This First

This baseline gives immediately useful production signals without scattering telemetry logic across many handlers:

- auth regressions become visible quickly
- image lifecycle issues become measurable
- account deletion traffic becomes visible
- the signals stay stable even if implementation details behind the route change

## Review Guidance

When a backend change touches one of the listed flows:

- confirm the flow is still classified correctly
- confirm the expected success/failure outcomes are still meaningful
- add a new flow tag if a new critical route is introduced

## Minimum Dashboard Panels

At minimum, the backend dashboard should expose these panels:

- auth flow events by `fooddiary.business_flow` and `fooddiary.business_outcome`
- request latency p95 and p99 by `url.path`
- request error volume by `url.path` and `http.response.status_code`
- output cache hit/miss by `fooddiary.output_cache.policy` and `fooddiary.output_cache.outcome`
- AI request outcomes by `fooddiary.ai.operation`, `fooddiary.ai.model`, and `fooddiary.ai.outcome`
- AI quota rejections by `fooddiary.ai.operation`
- AI fallback count by `fooddiary.ai.from_model` -> `fooddiary.ai.to_model`
- cleanup job execution outcome by `fooddiary.job.name` and `fooddiary.job.outcome`
- cleanup deleted-item volume by `fooddiary.job.name`
- cleanup job duration p95 by `fooddiary.job.name`
- database command failures by `fooddiary.db.operation`, `fooddiary.db.source`, and `error.type`
- email dispatch outcome by `fooddiary.email.template`, `fooddiary.email.locale`, and `fooddiary.email.outcome`
- storage presign/delete outcomes by `fooddiary.storage.operation` and `fooddiary.storage.outcome`
- external provider outcomes by `fooddiary.external_provider`, `fooddiary.external_provider.operation`, and `fooddiary.external_provider.outcome`
- external provider latency p95 by `fooddiary.external_provider` and `fooddiary.external_provider.operation`

## First Alert Suggestions

Use these as the first production alert baseline and tune them with real traffic data.

## Reliability SLO Baseline

Use the following initial objectives for a rolling 30-day production window.
They are starting targets and must be revised from observed traffic, not silently
relaxed when an alert becomes noisy.

| Capability | SLI | Initial objective | Page threshold |
| --- | --- | --- | --- |
| Authenticated API availability | successful non-4xx requests / eligible requests | 99.9% | 5-minute server-error ratio above 2% |
| High-value read latency | p95 duration for dashboard/products/recipes/consumptions | below 750 ms | p95 above 1.5 s for 10 minutes |
| Critical outbox delivery | time from record creation to processed state | 99% below 5 minutes | oldest pending message above 10 minutes |
| Critical outbox correctness | dead-lettered / claimed messages | below 0.1% | any dead letter for auth, billing, or account email; otherwise above 1% for 10 minutes |
| Recurring job freshness | time since last successful execution | schedule interval plus 5 minutes | two missed expected executions |
| Recurring job reliability | successful executions / completed executions | 99.5% | two consecutive failures or failure streak above 1 |

`fooddiary.outbox.messages` and `fooddiary.outbox.processing.duration` provide
delivery outcome and processing-duration evidence.
`fooddiary.outbox.pending.oldest_age` exports the current oldest pending age in
seconds after every processor run, tagged by `fooddiary.outbox.name`. Alert on
that gauge per outbox so a blocked email provider does not hide a healthy
image-deletion queue. Pair it with the job-freshness alert: if a processor stops
running entirely, its age observation cannot refresh but the missed job remains
visible through `fooddiary.job.last_success_age`.

For job freshness, use `fooddiary.job.last_success_age` and compare each series
with that job's configured schedule. Use `fooddiary.job.failure_streak` for the
consecutive-failure page. Dashboards should display the SLO, current value,
remaining error budget, and deploy markers together.

- Auth server errors:
  trigger when `fooddiary.api.business_flow.events` with `fooddiary.business_outcome=server_error` is non-zero for 5 minutes on any auth flow.
- Auth client-error spike:
  trigger when auth `client_error` volume increases sharply versus the previous 30-minute baseline after a deploy.
- AI provider degradation:
  trigger when `fooddiary.ai.requests` has repeated non-success outcomes for 10 minutes, especially `transport_error`, `timeout`, or `http_5xx`.
- External provider degradation:
  trigger when `fooddiary.external_provider.requests` has repeated `timeout`, `transport_error`, `http_error`, or `json_error` outcomes for 10 minutes, or when non-success outcomes exceed roughly 30-50% of requests for `open_food_facts`.
- AI quota exhaustion:
  trigger when `fooddiary.ai.quota_rejections` becomes non-zero in production for 10 minutes or exceeds the normal daily baseline.
- AI fallback spike:
  trigger when `fooddiary.ai.fallbacks` exceeds the normal baseline for 15 minutes, because that usually signals primary vision-model degradation.
- Output-cache regression:
  trigger when `fooddiary.api.output_cache.events` shows an unexpected drop in `hit` ratio for `PresentationUserScopedCache` or `PresentationAdminAiUsageCache` after a deploy.
- Cleanup job failure:
  trigger immediately when `fooddiary.job.execution.events` reports `fooddiary.job.outcome=failure`.
- Cleanup stagnation:
  trigger when `fooddiary.job.last_success_age` exceeds the expected cron interval plus grace period for a cleanup job.
- Cleanup repeated failure:
  trigger when `fooddiary.job.failure_streak` stays above `0` or grows across retries for a cleanup job.
- Database failure spike:
  trigger when `fooddiary.db.command.failures` becomes non-zero for 5 minutes on production or spikes materially after a deploy.
- Email delivery failure:
  trigger when `fooddiary.email.dispatch.events` reports repeated `failure` outcomes for `email_verification` or `password_reset` over 5-10 minutes.
- Storage boundary failure:
  trigger when `fooddiary.storage.operations` reports repeated `failure` outcomes for `presign` or `delete`, or when `validation_error` spikes after a deploy.
- Request latency regression:
  trigger when p95 or p99 request duration on high-value routes regresses materially versus the previous release baseline.

## Alert Triage Links

When an alert fires, jump to these runbooks first:

- auth and login/reset/refresh issues -> `BACKEND_RUNBOOKS.md` / `Authentication Incident`
- AI degradation or quota exhaustion -> `BACKEND_RUNBOOKS.md` / `Unavailable AI Provider`
- cleanup job failures -> `BACKEND_RUNBOOKS.md` / `Unavailable PostgreSQL` or `Unavailable S3-Compatible Storage`, depending on the failing dependency
- database failure spike -> `BACKEND_RUNBOOKS.md` / `Unavailable PostgreSQL`
- email delivery failure -> `BACKEND_RUNBOOKS.md` / `Authentication Incident`
- storage boundary failure -> `BACKEND_RUNBOOKS.md` / `Unavailable S3-Compatible Storage`
- cache hit-ratio regression -> check vary-by, auth, query-shape, and route metadata before treating it as a pure performance issue
- missing traces/metrics -> `BACKEND_RUNBOOKS.md` / `Telemetry Or Exporter Outage`

## Next Observability Candidates

- database retry counters
- output-cache hit ratio for admin and user-scoped cached endpoints
