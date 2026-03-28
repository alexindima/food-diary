# Backend Runbooks

Date: 2026-03-28
Scope: backend incident handling and operational recovery

## Purpose

This document captures the first operational runbook baseline for `B11` in `BACKEND_10_OF_10_PLAN.md`.

Use it to reduce decision latency during incidents and to keep recovery steps reproducible across environments.

## General Incident Rules

Apply these rules before using a scenario-specific runbook:

1. Stabilize first.
Do not stack risky changes during an active incident. Pause unrelated deploys, schema changes, and secret rotations until the current failure mode is understood.

2. Record exact scope.
Write down the affected environment, first observed time, current user impact, and any recent deploy or config change that likely correlates with the failure.

3. Prefer reversible actions.
If two actions can restore service, prefer the one that does not mutate persistent state irreversibly.

4. Keep evidence.
Preserve failing workflow logs, host logs, error IDs, and timestamps before restarting services or rotating credentials.

5. Close the loop after mitigation.
Every incident should leave behind one of:
   - a permanent code fix
   - a configuration change
   - a monitoring improvement
   - a runbook update

## Shared Signal Sources

During diagnosis, use these signals first:

- deploy workflow logs in GitHub Actions
- ASP.NET host logs
- PostgreSQL availability, connection, and migration state
- S3-compatible storage reachability and presign/upload behavior
- OpenTelemetry exporter or collector health
- business-flow counters documented in `BACKEND_OBSERVABILITY_BASELINE.md`
- auth and security expectations documented in `BACKEND_SECURITY_HARDENING.md`

Operational shortcut:

- if an alert already points to one of the dashboard panels or metric names from `BACKEND_OBSERVABILITY_BASELINE.md`, start there before broad log search

## Failed Deployment

Symptoms:

- deploy workflow fails
- new version is not healthy after rollout
- health checks fail immediately after app restart

Immediate actions:

1. Stop automatic retries and parallel redeploy attempts.
2. Identify the failing stage:
   - build/package
   - migration bundle
   - service restart
   - post-deploy health check
3. Keep the previous healthy version serving traffic if it is still available.

Diagnosis:

- Inspect GitHub Actions logs for the first failing step.
- If failure happened after migration execution, inspect database state before rolling back binaries.
- If failure happened during app startup, inspect host logs for missing config, secret, binding, or dependency errors.
- If failure happened only in health checks, compare environment-specific URLs, proxy headers, and secret injection with the previous healthy deployment.
- Compare cache hit ratio and auth business-flow outcomes before and after the deploy when the incident looks like a performance-only regression.

Recovery:

- Build/package failure:
  fix the pipeline or artifact issue and redeploy.
- Migration failure:
  switch to the failed migration runbook below.
- Startup/config failure:
  restore the last known-good configuration or redeploy the previous artifact if schema remains compatible.
- Health-check-only failure:
  correct the environment or routing issue, then rerun deploy validation.

Exit criteria:

- target environment responds healthy
- login or another high-value flow succeeds
- no migration step remains half-understood

## Failed Migration

Primary reference:

- `BACKEND_MIGRATION_SAFETY.md`

Symptoms:

- deploy fails on migration bundle execution
- app startup reports pending schema mismatch
- database errors begin immediately after schema rollout

Immediate actions:

1. Stop the rollout.
2. Do not edit already deployed migrations in place.
3. Determine whether the failure happened before, during, or after applying the migration.

Diagnosis:

- Inspect the `Run migration bundle on server` workflow step.
- Check `__EFMigrationsHistory`.
- Verify whether the database is:
  - still on the previous migration
  - partially changed by manual SQL or failing statements
  - already at the target migration while app rollout failed later

Recovery:

- No migration applied:
  fix the migration and redeploy.
- Migration applied successfully but app failed later:
  fix the app/deploy issue, do not roll back schema blindly.
- Migration failed mid-flight:
  prepare an explicit corrective migration or reviewed manual SQL fix, validate it, then redeploy.

Do not:

- change committed historical migration files after production exposure
- guess rollback SQL under time pressure without reviewing the exact schema state

Exit criteria:

- migration history is understood and consistent
- app and schema versions are compatible
- a follow-up action is recorded if manual SQL was required

## Unavailable PostgreSQL

Symptoms:

- API returns 5xx on most data-backed routes
- connection timeout or connection refused errors
- health checks fail after database dependency checks

Immediate actions:

1. Confirm whether the issue is total outage, network isolation, credential failure, or connection-pool exhaustion.
2. Pause non-essential admin or maintenance actions.
3. Treat migration and deploy actions as blocked until database reachability is restored.

Diagnosis:

- Verify the current `ConnectionStrings:DefaultConnection` source for the environment.
- Check whether the database host is reachable from the API runtime.
- Inspect PostgreSQL server health, disk space, max connections, and recent restarts.
- Inspect application logs for authentication failure versus transport failure versus timeout patterns.
- Check these metrics first:
  - `fooddiary.db.command.failures`
  - `fooddiary.api.business_flow.events`
  - `fooddiary.job.execution.events`
- Distinguish between:
  - read-path failures dominated by `fooddiary.db.operation=reader`
  - write-path failures dominated by `fooddiary.db.operation=non_query`
  - background or persistence-source failures via `fooddiary.db.source`
  - timeout/canceled/provider-exception patterns via `error.type`

Recovery:

- Credential/config issue:
  restore valid connection settings and restart the API host.
- Database host unavailable:
  recover the PostgreSQL instance or fail over according to the environment policy.
- Connection saturation:
  reduce load, investigate long-running queries, and restart only if saturation is not self-healing.
- Short transient transport issue:
  verify whether configured EF Core retries are masking brief failures, then fix the underlying instability instead of raising retry counts blindly.

Validation after recovery:

- one read path and one write path succeed
- auth refresh or login succeeds
- error rate returns to normal
- `fooddiary.db.command.failures` returns to the normal baseline

## Unavailable S3-Compatible Storage

Symptoms:

- upload URL generation fails
- image delete flow fails
- newly uploaded assets cannot be used reliably

Immediate actions:

1. Confirm whether the outage is in presign generation, upload execution, or delete operations.
2. Avoid bulk retries if the storage provider is returning sustained 5xx errors.

Diagnosis:

- Check `S3:ServiceUrl`, bucket configuration, region/path-style settings, and credentials.
- Verify current credential validity and whether a recent rotation occurred.
- Distinguish between:
  - backend cannot sign requests
  - client cannot upload to generated URL
  - backend cannot delete or reconcile assets later

Recovery:

- Credential/config issue:
  restore valid S3 settings and redeploy if needed.
- Provider outage:
  treat image upload as degraded and communicate limited functionality.
- Bucket/policy issue:
  restore the minimum required permissions for presign and delete paths.

Validation after recovery:

- upload URL generation succeeds
- a small allowed image uploads successfully
- delete flow succeeds for a test asset

## Unavailable AI Provider

Symptoms:

- AI-backed endpoints fail or time out
- quota or provider errors spike
- requests succeed in general but AI-specific features degrade

Immediate actions:

1. Classify whether the issue is provider outage, invalid API key, quota exhaustion, or model-level misconfiguration.
2. Keep core food-diary flows available; AI failure should not block unrelated routes.

Diagnosis:

- Check current `OpenAi:ApiKey` or equivalent provider secret source.
- Inspect host logs for auth failures, quota failures, and timeout patterns.
- Confirm whether the issue affects one environment or all environments.
- Check these metrics first:
  - `fooddiary.ai.requests`
  - `fooddiary.ai.quota_rejections`
  - `fooddiary.ai.fallbacks`
- Distinguish between:
  - quota exhaustion
  - transport failure
  - timeout
  - repeated `http_5xx`
  - repeated fallback activation on the primary vision model

Recovery:

- Secret/config issue:
  restore the key or provider settings and redeploy if required.
- Quota exhaustion:
  increase quota or temporarily reduce AI usage according to product policy.
- Provider outage:
  communicate degraded AI functionality and avoid repeated expensive retries.

Validation after recovery:

- a known AI-backed request succeeds
- non-AI flows remain healthy during and after mitigation
- AI request outcomes return to mostly `success`
- fallback and quota-rejection rates return to the normal baseline

## Authentication Incident

Symptoms:

- users cannot log in, refresh, restore, verify email, or reset password
- sudden spike in `401`, `429`, or auth flow server errors
- reports of forced logout or token invalidation

Immediate actions:

1. Identify the affected flow:
   - login
   - refresh
   - restore
   - email verification
   - password reset
   - admin SSO
   - Telegram auth
2. Check whether a secret rotation, deploy, proxy change, or rate-limit config change happened recently.

Diagnosis:

- Use business-flow counters from `BACKEND_OBSERVABILITY_BASELINE.md`.
- Check JWT, Telegram, SMTP, and SSO-related secrets depending on the failing flow.
- Verify trusted proxy configuration if rate limiting or client IP behavior changed.
- Confirm whether failures are client errors, token validation failures, or internal server errors.
- Compare:
  - `fooddiary.api.business_flow.events` for the affected auth flow
  - `fooddiary.email.dispatch.events` for `email_verification` and `password_reset` when the incident involves email-based recovery
  - request latency and request exception counters on matching routes
  - recent deploy time versus the first failure spike

Recovery:

- JWT secret issue:
  restore the intended signing secret or complete the planned hard cutover and communicate re-login requirement.
- SMTP issue affecting verification/reset:
  restore SMTP credentials and validate both mail flows.
- Rate-limit false positives:
  review trusted proxy settings and limiter configuration before loosening limits globally.
- Admin SSO issue:
  validate cache, code TTL behavior, and current admin role resolution.

Validation after recovery:

- login succeeds
- refresh succeeds
- one email-based recovery flow succeeds
- no unexpected spike remains in auth rate-limit rejections
- auth business-flow counters return to expected success/error ratios

## Telemetry Or Exporter Outage

Symptoms:

- traces or metrics disappear from dashboards
- exporter connection errors in logs
- service is healthy but observability is blind

Immediate actions:

1. Confirm whether only telemetry delivery is failing or whether the API itself is degraded.
2. Do not treat observability loss as proof of app outage without checking direct health signals.

Diagnosis:

- Check OpenTelemetry exporter endpoint, credentials, and collector availability.
- Verify whether host logs still exist even if traces/metrics export is failing.
- Confirm whether failures began after deploy or after telemetry backend changes.

Recovery:

- exporter/collector unavailable:
  restore the collector or switch to the valid endpoint.
- credential/config issue:
  restore telemetry settings and restart the host if needed.
- noisy retry loop:
  reduce exporter pressure if it risks affecting request handling.

Validation after recovery:

- traces or metrics resume
- backend request latency and error rate stay normal during recovery
- at least one auth flow, one AI flow, and one cleanup job metric appears again in the backend dashboard
- output-cache hit/miss events appear again for cache-enabled routes after representative traffic

## Cleanup Job Incident

Symptoms:

- scheduled cleanup job stops running
- cleanup job failure alert fires
- deleted-item backlog grows unexpectedly

Immediate actions:

1. Identify the failing job:
   - `images.cleanup`
   - `users.cleanup`
2. Check whether the latest execution failed or whether the scheduler stopped enqueuing entirely.
3. Do not rerun the job repeatedly until the failing dependency is understood.

Diagnosis:

- Check:
  - `fooddiary.job.execution.events`
  - `fooddiary.job.deleted_items`
  - `fooddiary.job.execution.duration`
- Distinguish between:
  - scheduler issue in `FoodDiary.JobManager`
  - PostgreSQL dependency issue
  - S3 delete issue
  - data-volume regression causing long runtimes
- Review the last successful execution time and compare it with the configured cron schedule.

Recovery:

- Scheduler not enqueuing:
  restore Hangfire/host execution and confirm recurring job registration.
- Database failure:
  switch to `Unavailable PostgreSQL`.
- Storage delete issue during image or user cleanup:
  switch to `Unavailable S3-Compatible Storage`.
- Runtime regression:
  reduce batch size temporarily or pause the affected cleanup path until the data-path issue is understood.

Validation after recovery:

- the affected cleanup job emits a successful execution event
- deleted-item count moves again in the expected direction
- execution duration is back within the normal operating range

## Post-Incident Template

Record these items after each incident:

- incident title
- environment
- start time and end time
- customer-visible impact
- trigger or suspected cause
- exact mitigation used
- follow-up actions
- owner and due date for follow-up
