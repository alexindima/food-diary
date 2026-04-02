# Project Top-10 Roadmap

Date: 2026-04-02
Scope: backend + frontend

## Overall Assessment

Current project level:

- Backend: about 8.5/10
- Frontend: about 7/10

The project is already strong in architecture and engineering discipline. The main gap to "very good" is no longer the base structure. It is the last layer of product reliability, frontend consistency, operational maturity, and reduction of concentrated complexity.

## Backend Top-10

### 1. Split `User` responsibilities

Priority: Critical

`User` currently holds too many concerns:

- authentication state
- security tokens
- profile
- goals
- AI quotas
- dashboard preferences
- soft-delete state

Why it matters:

- increases aggregate complexity
- makes invariants harder to reason about
- raises risk of cross-feature regression inside one type

Suggested direction:

- separate auth/security behavior from profile/goals/preferences
- if full split is too expensive now, first extract internal state objects and behavior boundaries

Reference:

- `FoodDiary.Domain/Entities/Users/User.cs`

### 2. Finalize one time policy across layers

Priority: High

The backend is much better than average here, but it still has two time models:

- `IDateTimeProvider` in application
- `DomainTime.UtcNow` in domain

Why it matters:

- unclear ownership of wall-clock time
- more edge cases in tests and business rules
- easier to introduce inconsistency later

Suggested direction:

- explicitly define when time may be read in domain
- document the rule in one place
- add guardrail tests for the chosen approach

References:

- `FoodDiary.Application/Common/Interfaces/Services/IDateTimeProvider.cs`
- `FoodDiary.Domain/Common/Entity.cs`

### 3. Formalize soft-delete invariants

Priority: High

Soft-delete behavior looks partly enforced, but it should be made fully explicit.

Questions to settle:

- what mutations are forbidden after delete
- what can be restored
- what data must be purged
- what data may be reassigned

Suggested direction:

- define a soft-delete state matrix
- cover it with domain, application, and integration tests

Reference:

- `FoodDiary.Domain/Entities/Users/User.cs`

### 4. Strengthen auth/security model boundaries

Priority: High

Authentication and security logic are implemented seriously, but the model is still too coupled to the general user aggregate.

Suggested direction:

- isolate account credential state
- isolate recovery / verification token lifecycle
- make auth transitions easier to audit independently

References:

- `FoodDiary.Application/Authentication/`
- `FoodDiary.Domain/Entities/Users/User.cs`

### 5. Add explicit performance baselines for critical endpoints

Priority: Medium-High

Observability exists, but "very good" requires measurable expectations.

Suggested direction:

- define hot endpoints
- record baseline latency and failure rate targets
- add focused performance checks for expensive read paths

References:

- `BACKEND_PERFORMANCE_REVIEW.md`
- `FoodDiary.Web.Api/Extensions/ApiServiceCollectionExtensions.cs`

### 6. Formalize API contract governance further

Priority: Medium-High

OpenAPI and payload snapshots are already a strong base.

Next step:

- define a breaking-change policy
- define API change workflow for consumers
- keep contract changes intentional and reviewable

References:

- `BACKEND_API_CONTRACT_GOVERNANCE.md`
- `tests/FoodDiary.Web.Api.IntegrationTests/Snapshots/`

### 7. Expand idempotency and consistency strategy for writes

Priority: Medium

There is already an `IdempotencyFilter`, which is a good sign. The policy should become more explicit across write operations.

Suggested direction:

- classify write commands by idempotency behavior
- define retry expectations
- add tests for duplicate requests where business-critical

References:

- `FoodDiary.Presentation.Api/Filters/IdempotencyFilter.cs`
- `FoodDiary.Presentation.Api/Extensions/PresentationServiceCollectionExtensions.cs`

### 8. Keep query shaping disciplined as features grow

Priority: Medium

The architecture is clean now, but feature-rich CRUD systems often decay in query boundaries over time.

Suggested direction:

- keep application query handlers thin and intentional
- avoid presentation-driven query leakage into infrastructure
- review large read models periodically

References:

- `FoodDiary.Application/`
- `FoodDiary.Infrastructure/Persistence/`

### 9. Raise operational maturity for jobs and cleanup flows

Priority: Medium

Background processing exists, but "very good" requires clearer operating semantics.

Suggested direction:

- define retry strategy
- define failure visibility
- define replay/recovery rules
- define poison-case handling

References:

- `FoodDiary.JobManager/`
- `BACKEND_RUNBOOKS.md`

### 10. Turn security review into a recurring engineering practice

Priority: Medium

The backend already has a good foundation:

- JWT validation
- rate limiting
- forwarded header hardening
- secret placeholders

Next step:

- maintain a recurring review checklist for:
  - auth flows
  - admin flows
  - file upload paths
  - public asset exposure
  - Telegram integration
  - abuse scenarios

References:

- `BACKEND_SECURITY_HARDENING.md`
- `FoodDiary.Web.Api/Extensions/ApiApplicationBuilderExtensions.cs`

## Frontend Top-10

### 1. Fix the SignalR email verification hub URL bug

Priority: Critical

The realtime service strips `/api/auth`, but environment URLs use `/api/v1/auth`.

Why it matters:

- email verification realtime may point to the wrong endpoint
- production behavior may be fragile or environment-dependent

Reference:

- `FoodDiary.Web.Client/src/app/features/auth/lib/email-verification-realtime.service.ts`

### 2. Properly wire global error handling

Priority: High

There is a global error handler class, but it does not currently look like a first-class runtime system.

Suggested direction:

- explicitly register Angular `ErrorHandler`
- make client exception reporting deterministic
- decide what is sent to backend and what is only shown locally

References:

- `FoodDiary.Web.Client/src/app/services/error-handler.service.ts`
- `FoodDiary.Web.Client/src/app/app.config.ts`

### 3. Replace scattered `console.error` / `console.warn` handling

Priority: High

Many services and pages log errors ad hoc.

Why it matters:

- inconsistent UX
- inconsistent telemetry
- harder to understand actual failure modes

Suggested direction:

- introduce one frontend error mapping strategy
- separate user-facing messages from diagnostics
- centralize toast/report/logging policy

References:

- `FoodDiary.Web.Client/src/app/services/auth.service.ts`
- `FoodDiary.Web.Client/src/app/features/dashboard/api/dashboard.service.ts`
- `FoodDiary.Web.Client/src/app/shared/api/`

### 4. Reduce manual orchestration in large pages

Priority: High

Several pages already carry too much data-loading and state-coordination logic.

Main candidates:

- dashboard
- shopping lists
- meal management
- product management

Suggested direction:

- move orchestration into facade/store/resource layers
- keep components focused on rendering and interaction

References:

- `FoodDiary.Web.Client/src/app/features/dashboard/pages/dashboard.component.ts`
- `FoodDiary.Web.Client/src/app/features/shopping-lists/pages/shopping-list-page.component.ts`

### 5. Raise admin frontend test coverage

Priority: High

The admin app is under-tested compared to the main client.

Suggested direction:

- add feature-level unit tests for admin pages and dialogs
- cover admin auth, users, AI usage, and email templates

Reference:

- `FoodDiary.Web.Client/projects/fooddiary-admin/src/app/`

### 6. Add E2E smoke coverage for critical user flows

Priority: High

The repository currently does not show a browser E2E test layer.

Suggested minimum smoke flows:

- register and login
- create product
- create meal
- update profile
- shopping list flow
- password reset flow
- admin sign-in and core admin path

Reference:

- `FoodDiary.Web.Client/`

### 7. Tighten remaining `any` usage in shared abstractions

Priority: Medium

Most of the frontend is reasonably typed, but some base abstractions still use `any`.

Suggested direction:

- remove `any` from base API and error paths first
- keep strict typing strongest in shared layers

References:

- `FoodDiary.Web.Client/src/app/services/api.service.ts`
- `FoodDiary.Web.Client/src/app/services/error-handler.service.ts`

### 8. Strengthen localization quality gates

Priority: Medium

Localization exists and the repository already enforces RU/EN updates in process, but quality should be more automated.

Suggested direction:

- detect missing translation keys
- verify both locales for critical screens
- verify RU rendering and language-specific edge cases

References:

- `FoodDiary.Web.Client/assets/i18n/en.json`
- `FoodDiary.Web.Client/assets/i18n/ru.json`

### 9. Add frontend observability and UX telemetry

Priority: Medium

To become "very good", the frontend needs production visibility comparable to the backend.

Suggested direction:

- track route timings
- track API error rates
- track web vitals
- track client exceptions

References:

- `FoodDiary.Web.Client/src/app/services/logging-api.service.ts`
- `FoodDiary.Web.Client/src/app/services/error-handler.service.ts`

### 10. Push UX polish from good to excellent

Priority: Medium

The architecture is good enough that product polish becomes a leverage point.

Focus areas:

- skeleton and loading states
- empty states
- optimistic updates where safe
- mobile consistency
- poor-network behavior
- dialog and form consistency

References:

- `FoodDiary.Web.Client/src/app/features/`
- `FoodDiary.Web.Client/projects/fd-ui-kit/src/lib/`

## Recommended Execution Order

### Phase 1: Immediate reliability

1. Fix SignalR hub URL
2. Register and standardize global frontend error handling
3. Remove debug logging and unify error policy

Status update:

- Completed on 2026-04-02
- SignalR verification hub URL fixed
- global error handling wired with environment-aware behavior
- scattered frontend error logging reduced and standardized

### Phase 2: Frontend quality maturity

1. Add admin tests
2. Add browser E2E smoke tests
3. Start reducing heavy page orchestration

Status update:

- Mostly completed on 2026-04-02
- admin feature tests added
- smoke E2E scaffold added for admin flows
- localization consistency checks added and wired into hooks/CI

### Phase 3: Backend domain and policy maturity

1. Split or internally restructure `User`
2. Finalize one time policy
3. Formalize soft-delete and auth/security state rules

Status update:

- Started on 2026-04-02
- application time provider unified with domain time
- `User` internally restructured further via `UserAccountState`
- authentication lifecycle policy centralized across key auth entry points

### Phase 4: Excellence layer

1. Add stronger frontend observability
2. Add performance budgets and baselines
3. Raise UX polish and operational maturity

Status update:

- Frontend performance work already started ahead of phase ordering
- initial frontend bundle reduced from about `1.43 MB` to `977.58 kB`
- production frontend build now passes without bundle budget warning
- backend performance baseline work started
- PostgreSQL-backed endpoint latency budgets now cover `auth.refresh`, `GET /products`, `GET /recipes`, and `POST /images/upload-url`
- next backend perf gap is a seeded `GET /consumptions` latency gate

## Definition Of "Very Good"

The project can be considered "very good" when:

- frontend reliability is as disciplined as backend reliability
- critical user flows are covered by E2E smoke tests
- admin app is no longer weakly tested
- `User` aggregate complexity is reduced
- time and delete policies are explicit and enforced
- observability exists on both backend and frontend
- UX consistency is strong on desktop and mobile
