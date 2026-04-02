# Project Execution Roadmap

Date: 2026-04-02
Scope: backend + frontend
Source strategy: `PROJECT_TOP10_ROADMAP_2026-04-02.md`

## Goal

Move the project from "already strong" to "very good" through a sequence of small, finishable phases.

## Working Rules

- Do not open too many fronts at once.
- Finish reliability and test maturity before major refactors.
- Each task must have a concrete done condition.
- Prefer changes that reduce future complexity, not only patch symptoms.

## Phase 1: Frontend Reliability

Target outcome:

- critical frontend runtime issues are removed
- error reporting is wired consistently
- known fragile points are stabilized

Tasks:

1. Fix email verification SignalR hub URL construction
Done when:
- realtime service builds the correct hub URL from `/api/v1/auth`
- a unit test covers the URL transformation

2. Wire global Angular error handling explicitly
Done when:
- `GlobalErrorHandler` is registered in app runtime
- browser global error listeners are enabled
- client exceptions are sent through one path

3. Replace ad hoc debug logging in critical auth/runtime paths
Done when:
- debug `console.log` is removed from high-signal runtime paths
- crash/error logging remains intentional

4. Review critical startup flow
Done when:
- localization init
- auth init
- current-user bootstrap
- service worker registration
all have explicit ownership and no hidden failure path

## Phase 2: Frontend Test Maturity

Target outcome:

- the frontend becomes harder to regress silently
- admin app is no longer the weak link

Tasks:

1. Add admin feature unit tests
Done when:
- admin dashboard
- admin users
- admin AI usage
- admin email templates
have baseline page/service/dialog tests

2. Add browser E2E smoke tests
Done when these flows are covered:
- register/login
- create product
- create meal
- update profile
- shopping list lifecycle
- password reset
- admin entry flow

3. Add localization quality checks
Done when:
- missing translation keys are detectable
- RU/EN critical pages are validated in tests or automated checks

## Phase 3: Frontend Structure Cleanup

Target outcome:

- pages become thinner
- side effects become easier to reason about
- state handling becomes more deliberate

Tasks:

1. Introduce facade/store/resource layer for dashboard
Done when:
- page is mostly view orchestration
- data loading and side effects move out of the component

2. Apply the same approach to shopping lists
Done when:
- save queue, loading, and mutation flow no longer live mostly in the page component

3. Reduce manual `subscribe()` usage in large features
Done when:
- the heaviest pages stop coordinating multiple subscriptions directly

4. Remove `any` from shared frontend abstractions first
Done when:
- shared services and base abstractions stop using `any` in core paths

## Phase 4: Backend Domain Maturity

Target outcome:

- backend complexity shifts from implicit to explicit
- core domain rules become easier to maintain safely

Tasks:

1. Restructure `User`
Done when:
- auth/security behavior is separated from profile/goals/preferences at least internally
- the aggregate surface becomes easier to audit

2. Finalize one time policy
Done when:
- time ownership is documented
- guardrail tests protect the policy

3. Formalize soft-delete rules
Done when:
- allowed and forbidden transitions are documented
- application/integration tests cover them

4. Review auth/security transitions
Done when:
- token lifecycle
- restore flow
- delete/restore constraints
- admin-sensitive auth paths
are explicitly verified

## Phase 5: Excellence Layer

Target outcome:

- the project is not only correct, but also observable and polished

Tasks:

1. Add frontend observability
Done when:
- client exceptions
- route timings
- API error rates
- key UX timings
can be monitored

2. Define backend performance baselines
Done when:
- hot endpoints have target latency/error budgets
- expensive paths are known and tracked

3. Raise UX consistency
Done when:
- empty states
- skeleton states
- mobile behavior
- dialogs and forms
feel consistent across the main product

4. Raise job and incident operational maturity
Done when:
- retries, failure visibility, and recovery rules are explicit for background processes

## Suggested Delivery Rhythm

### Next 2 weeks

- finish Phase 1
- start admin unit tests from Phase 2

### Next 1 month

- complete Phase 2
- begin dashboard/shopping-list cleanup in Phase 3

### Next 3 months

- complete Phase 3
- complete the highest-value items from Phase 4
- begin selected items from Phase 5

## Current Recommended Focus

Current active phase: Phase 4

Current tasks to execute first:

1. Decide next major backend domain slice after lifecycle/auth improvements
2. Choose whether to continue backend policy maturity or switch to frontend observability
3. Keep future work grouped into one major block at a time

## Progress Snapshot

### Completed on 2026-04-02

- Phase 1: Frontend Reliability
- Phase 2: Frontend Test Maturity baseline
- Phase 3: Frontend Structure Cleanup core screens

### Backend progress completed on 2026-04-02

- unified application time provider with domain time
- extracted `UserAccountState` from the `User` aggregate internals
- enforced lifecycle checks in refresh-token flow
- centralized authentication user access policy for core auth entry points
- added PostgreSQL-backed endpoint latency baselines for `auth.refresh`, `products`, `recipes`, and `images.upload-url`
- expanded PostgreSQL-backed endpoint latency baselines to include `consumptions` meal-list paging

### Frontend progress completed on 2026-04-02

- env-aware global error handling in main app and admin app
- shared frontend API error policy cleanup
- admin feature tests baseline
- admin smoke E2E scaffold
- i18n consistency check wired into hooks and CI
- facade cleanup for dashboard, shopping lists, products, meals, statistics, profile, goals, weight history, waist history, cycle tracking, recipes
- initial bundle reduced below budget threshold

## Suggested Next Decision

Pick one of these and keep it isolated:

1. Backend domain continuation
Done when:
- next `User`-related or account-lifecycle slice is completed and tested

2. Frontend observability
Done when:
- client exceptions and route/API metrics have a defined collection path

3. Backend performance baselines
Done when:
- hot endpoints and their latency targets are documented and tested

Current status:
- in progress
- repository-level latency budgets exist for product and recipe paging
- endpoint-level PostgreSQL baselines now exist for `auth.refresh`, `GET /products`, `GET /recipes`, `GET /consumptions`, and `POST /images/upload-url`
- next backend perf task is `EXPLAIN ANALYZE` capture for the heaviest product, recipe, and meal queries
