# Backend 10/10 Plan

Date: 2026-03-28
Scope: `FoodDiary.Web.Api`, `FoodDiary.Presentation.Api`, `FoodDiary.Application`, `FoodDiary.Domain`, `FoodDiary.Infrastructure`, backend test projects

## Goal

Use this document as the working backend roadmap until the backend reaches a stable 10/10 level in architecture, reliability, security, observability, and operational maturity.

This is not a rewrite plan.
The current backend is already strong.
The target is to remove the remaining quality ceilings in a controlled order.

## Current Assessment

Current score: `8.5/10`

Current strengths:

- Clear layered architecture with a clean composition root
- Feature-first organization across core backend layers
- Architecture tests that protect layering and conventions
- Strong application and domain test coverage
- Existing PostgreSQL-backed integration coverage for critical API paths
- Good startup validation for options and host policies
- Reasonable telemetry, rate limiting, output cache, and presentation boundary discipline

Main gaps to close:

- Repository-tracked configuration still contains sensitive or production-oriented values
- Time handling policy is not fully unified across layers
- Critical backend integration matrix is good but should be formalized and enforced
- API contract stability needs explicit verification
- Observability should include business and operational signals, not only HTTP-level telemetry
- Performance, migration safety, and runbook maturity still need a deliberate pass
- Some domain areas, especially `User`, can still be simplified and narrowed

## Working Principles

- Prefer incremental improvement over broad rewrites
- Keep dependency direction inward
- Preserve existing feature-first structure
- Add quality gates before large refactors where possible
- Treat production safety and testability as first-class outcomes
- Update this file as work progresses

## Roadmap

### Days 1-30

Focus: remove the highest-risk gaps and define the operating rules for future backend work.

#### 1. Config and Secret Hygiene

Priority: `P0`

Goals:

- Remove real or sensitive values from repository-tracked config
- Keep only safe defaults and templates in source control
- Make local/dev/prod configuration boundaries explicit

Target areas:

- `FoodDiary.Web.Api/appsettings.json`
- typed options in `FoodDiary.Web.Api/Options/`
- typed options in `FoodDiary.Infrastructure/Options/`

Deliverables:

- Safe repository config with no real passwords or operational secrets
- `appsettings.Template.json` or equivalent bootstrap template
- Startup validation for required non-dev secrets where practical
- Short configuration guide for local setup and deployment expectations

Definition of done:

- No real secrets or local passwords remain in tracked backend config
- Backend can still start locally with documented setup steps
- Configuration failures are explicit and early

#### 2. Unified Time Policy

Priority: `P0`

Goals:

- Define one time-handling policy across backend layers
- Reduce direct `DateTime.UtcNow` usage outside approved infrastructure boundaries
- Improve determinism of tests and time-sensitive business logic

Target areas:

- `FoodDiary.Application`
- `FoodDiary.Presentation.Api`
- `FoodDiary.Domain`
- `FoodDiary.Infrastructure`

Deliverables:

- Short written policy for time ownership by layer
- `IDateTimeProvider` usage expanded where appropriate
- Direct clock usage reduced to explicitly approved locations
- Tests for UTC normalization and time-dependent behavior

Definition of done:

- New backend code follows one consistent time policy
- Existing high-risk time paths are migrated
- Time-sensitive tests are stable and readable

#### 3. Critical Backend Flow Matrix

Priority: `P0`

Goals:

- Define which backend flows are production-critical
- Ensure each critical flow has integration coverage against PostgreSQL
- Make the set visible and enforceable in CI

Target areas:

- `tests/FoodDiary.Web.Api.IntegrationTests`
- `tests/FoodDiary.Infrastructure.Tests`

Initial critical flows:

- registration and authenticated access
- refresh token rotation
- password reset and email verification lifecycle
- user soft-delete and restore
- product, recipe, shopping list relational behavior
- image asset lifecycle and reference protection
- critical migration paths

Deliverables:

- Written critical flow checklist
- Missing PostgreSQL integration tests added
- CI expectation for critical-flow coverage documented

Definition of done:

- Critical flow list exists in repository
- Each listed flow is covered by an integration test or explicitly deferred with rationale
- Failing critical paths are visible in CI

#### 4. Backend Definition of Done

Priority: `P0`

Goals:

- Make backend quality repeatable
- Reduce reliance on memory and ad hoc reviewer expectations

Deliverables:

- Backend DoD section in repo docs or team checklist
- Required checks for backend changes documented

Recommended DoD items:

- relevant unit/application tests
- PostgreSQL integration coverage for critical behavior changes
- migration review when schema changes
- config review when options or secrets change
- telemetry review when user-visible flows change
- contract review when API payloads or status codes change

Definition of done:

- The team can use one documented checklist for backend changes

### Days 31-60

Focus: harden the API surface, production diagnostics, and data-path safety.

#### 5. API Contract Governance

Priority: `P1`

Goals:

- Treat the HTTP contract as a protected artifact
- Detect accidental breaking changes before merge

Target areas:

- `FoodDiary.Presentation.Api`
- Swagger/OpenAPI setup in `FoodDiary.Web.Api`
- presentation tests in `tests/FoodDiary.Presentation.Api.Tests`

Deliverables:

- Stable OpenAPI generation in CI
- Contract snapshot or diff check
- Explicit rules for naming, nullable fields, status codes, and error payloads

Definition of done:

- Contract-breaking backend changes are visible before release
- Error and response conventions are documented and enforced

#### 6. Observability Expansion

Priority: `P1`

Goals:

- Extend observability beyond HTTP request timing
- Make backend incidents diagnosable from metrics and traces

Target areas:

- `FoodDiary.Web.Api/Extensions/`
- `FoodDiary.Presentation.Api/Extensions/`
- background jobs and infrastructure services where relevant

Recommended signals:

- registration success and failure rate
- login and refresh failure rate
- AI quota usage and rejection rate
- image upload URL generation and delete failures
- background cleanup outcomes
- DB retry/failure signals
- p95 and p99 latency by endpoint group
- output cache hit ratio where relevant

Deliverables:

- Additional metrics and trace tags
- Dashboard definitions
- Alert thresholds for the most important failure modes

Definition of done:

- The most important backend incidents can be detected and triaged from telemetry

#### 7. Performance and Query Review

Priority: `P1`

Goals:

- Establish a baseline for backend performance
- Identify slow queries, hot endpoints, and index gaps

Target areas:

- `FoodDiary.Infrastructure/Persistence`
- high-traffic presentation endpoints
- heavy read and write scenarios

Deliverables:

- Short performance baseline for critical endpoints
- Review of hot EF queries and index usage
- List of fixes for heavy includes, N+1 patterns, or avoidable allocations

Definition of done:

- Known critical endpoints have measured latency targets
- High-impact data-path issues are fixed or explicitly tracked

#### 8. Migration Safety

Priority: `P1`

Goals:

- Ensure schema evolution is safe in realistic environments
- Reduce release risk from database changes

Target areas:

- `FoodDiary.Infrastructure` migrations
- database bootstrap and upgrade test flows

Deliverables:

- Migration validation on clean PostgreSQL
- Migration validation on upgraded PostgreSQL state where practical
- Rollback or incident guidance for failed migrations

Definition of done:

- Migrations are tested, not only generated
- Release instructions exist for schema-affecting changes

### Days 61-90

Focus: remove remaining architectural drag and harden operational maturity.

#### 9. Security Hardening Pass

Priority: `P2`

Goals:

- Review backend attack surface systematically
- Tighten auth, admin, and integration boundaries

Target areas:

- auth and refresh token flows
- admin SSO
- Telegram auth flows
- image upload flow
- CORS and rate-limiting policy
- dependency vulnerability posture

Deliverables:

- Focused backend threat model
- Action list for identified security gaps
- Secret rotation expectations documented

Definition of done:

- Highest-value auth and admin risks are explicitly reviewed and addressed

#### 10. Domain Simplification

Priority: `P2`

Goals:

- Reduce aggregate overload in complex domain types
- Keep invariants easier to reason about and test

Primary target:

- `FoodDiary.Domain/Entities/Users/User.cs`

Deliverables:

- Clearer responsibility split inside or around `User`
- Reduced cognitive load in security/profile/goals/preferences behavior
- Regression tests for refactored invariants

Definition of done:

- `User` behavior is easier to understand, test, and extend without accidental coupling

#### 11. Runbooks and Operational Readiness

Priority: `P2`

Goals:

- Make backend operation reproducible during incidents
- Reduce dependence on tribal knowledge

Recommended runbooks:

- failed deployment
- failed migration
- unavailable PostgreSQL
- unavailable S3-compatible storage
- unavailable AI provider
- authentication incident
- telemetry/exporter outage

Deliverables:

- Short operator-facing markdown docs
- Links to metrics, logs, and recovery actions

Definition of done:

- Common backend incidents have written operating instructions

#### 12. Performance Regression Gates

Priority: `P3`

Goals:

- Prevent slow degradation over time
- Turn performance expectations into a maintained quality gate

Deliverables:

- Performance budgets for selected critical endpoints
- Repeatable perf checks or benchmark scripts
- Regression review process for threshold violations

Definition of done:

- Backend performance degradation becomes visible and actionable

## Priority Order

1. Config and secret hygiene
2. Unified time policy
3. Critical backend flow matrix
4. Backend definition of done
5. API contract governance
6. Observability expansion
7. Performance and query review
8. Migration safety
9. Security hardening pass
10. Domain simplification
11. Runbooks and operational readiness
12. Performance regression gates

## Tracking

Use this section as the live execution tracker.

### Work Items

- [x] `B01` `P0` Config and secret hygiene
  Notes: remove sensitive values from tracked config, add safe template, keep startup validation explicit.
- [x] `B02` `P0` Unified time policy
  Notes: define one backend policy for time ownership and reduce direct `DateTime.UtcNow` usage.
  Progress: policy document added in `BACKEND_TIME_POLICY.md`; `Application`, `Presentation`, `Infrastructure`, and domain hot spots were migrated. Remaining direct `DateTime.UtcNow` usage is limited to `SystemDateTimeProvider` and generated migration artifacts.
- [x] `B03` `P0` Critical backend flow matrix
  Notes: define mandatory PostgreSQL-backed integration coverage for critical paths.
  Progress: matrix documented in `BACKEND_CRITICAL_FLOW_MATRIX.md`; PostgreSQL critical auth coverage expanded for refresh, restore, and password-reset request; password-reset confirm remains explicitly deferred with rationale.
- [x] `B04` `P0` Backend definition of done
  Notes: document required quality gates for backend changes.
  Progress: checklist documented in `BACKEND_DEFINITION_OF_DONE.md` and linked from the root `README.md`.
- [x] `B05` `P1` API contract governance
  Notes: protect OpenAPI and detect breaking contract changes before merge.
  Progress: existing snapshot-based contract protections were formalized in `BACKEND_API_CONTRACT_GOVERNANCE.md`; auth/admin OpenAPI snapshot coverage was expanded to include restore and password-reset routes.
- [x] `B06` `P1` Observability expansion
  Notes: add business and operational metrics, dashboards, and alerts.
  Progress: first observability baseline documented in `BACKEND_OBSERVABILITY_BASELINE.md`; host-level business-flow counter added for auth, image, and user-deletion routes with outcome tagging and tests.
- [x] `B07` `P1` Performance and query review
  Notes: measure hot paths, review EF queries, validate indexes and latency budgets.
  Progress: documented the baseline in `BACKEND_PERFORMANCE_REVIEW.md`; replaced `ToLower().Contains(...)` search in `ProductRepository` and `RecipeRepository` with escaped PostgreSQL `ILIKE`; added composite indexes for product, recipe, and meal paging paths; added PostgreSQL integration coverage for escaped search behavior.
- [x] `B08` `P1` Migration safety
  Notes: validate schema changes on realistic PostgreSQL paths and document recovery steps.
  Progress: migration safety documented in `BACKEND_MIGRATION_SAFETY.md`; PostgreSQL integration coverage added for `clean -> latest` and `initial -> latest` migration paths using isolated databases and EF migrator.
- [x] `B09` `P2` Security hardening pass
  Notes: review auth, admin, upload flows, rate limiting, CORS, and dependency posture.
  Progress: `BACKEND_SECURITY_HARDENING.md` now covers the current security baseline, focused threat model, and secret-rotation expectations; auth/admin/upload abuse-prone endpoints carry explicit rate-limit contracts and OpenAPI coverage; refresh token rotation-on-use was added with unit and PostgreSQL-backed coverage for replay rejection; rate limiting no longer trusts raw `X-Forwarded-For` without explicit proxy trust configuration; typed forwarded-header trust options were added for known proxies/networks and wired into the host pipeline before logging; host config/docs describe reverse-proxy setup explicitly; `dotnet list package --vulnerable --include-transitive` was run for `FoodDiary.Web.Api` and `FoodDiary.Infrastructure` with no reported vulnerable packages from the configured sources.
- [x] `B10` `P2` Domain simplification
  Notes: reduce aggregate overload, especially around `User`.
  Progress: first slices started by introducing `UserGoalUpdate` so bulk goal updates no longer flow through a long nullable parameter list from application code, by introducing `User.SetActive(bool)` so lifecycle toggles no longer branch manually in multiple application handlers, by introducing explicit `User.SetLanguage(string)` so application no longer routes language-only changes through `UpdatePreferences(...)`, by introducing `UserAiTokenLimitUpdate` so admin AI limit updates no longer pass raw primitive pairs directly into handler logic, by introducing `UserAdminAccountUpdate` so admin-controlled email-confirmed/language/AI-limit changes now execute as one narrow domain operation instead of three separate handler-level mutations, by introducing `UserAdminUpdate` so `UpdateAdminUserCommandHandler` now applies lifecycle/account/role changes through one aggregate-level admin update call after repository role resolution, and by introducing credential/lifecycle helpers `User.CompletePasswordReset(...)`, `User.CompleteEmailVerification(...)`, and `User.DeleteAccount(...)` so application handlers no longer orchestrate those paired state transitions manually. Dead legacy wrappers `ConfirmEmail()` and `ClearPasswordResetToken()` have also been removed to reduce aggregate surface. Explicit desired-weight/waist clear semantics remain isolated in dedicated domain methods. `UpdateUserCommandHandler` intentionally still uses narrow profile/activity/media/preference operations because architecture rules explicitly prohibit the wide `user.UpdateProfile(...)` facade from the application layer.
- [x] `B11` `P2` Runbooks and operational readiness
  Notes: documented recovery steps for failed deploy, failed migration, PostgreSQL outage, S3 outage, AI provider outage, authentication incident, and telemetry/exporter outage in `BACKEND_RUNBOOKS.md`; linked runbooks to existing observability, migration safety, and security baseline documents.
- [x] `B12` `P3` Performance regression gates
  Notes: added repeatable PostgreSQL-backed performance gates for `ProductRepository.GetPagedAsync` and `RecipeRepository.GetPagedAsync` first-page reads with a `1500`-row seed and `250 ms` latency budget; documented the thresholds and regression-review expectation in `BACKEND_PERFORMANCE_REVIEW.md`.

## Next Suggested Step

Treat the backend plan as complete and move to normal maintenance, with future work tracked as normal backlog rather than as unfinished plan items.

Reason:

- `B09` through `B12` are now covered by code, tests, and operational baseline documents
- the remaining ideas are incremental follow-ups such as broader performance benchmarking, additional search tuning, and ongoing runbook/security doc maintenance
