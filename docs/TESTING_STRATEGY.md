# Testing Strategy

Notifications-focused application, aggregate and provider/persistence tests live under `Modules/Notifications/tests`. Central projects retain HTTP/host, shared DbContext/Postgres and cross-module DI coverage.

## Fast Architecture Feedback
Run architecture tests when changing project references, folders, boundary rules, controllers, async method conventions, or service client packages.

```bash
dotnet test Tooling/tests/FoodDiary.ArchitectureTests/FoodDiary.ArchitectureTests.csproj
```

These tests are also the best executable documentation for backend boundaries.
They also guard the allowed reference graph between test projects, so shared test helpers should be added through `Tooling/FoodDiary.Testing` instead of ad hoc cross-test-project references.

## Local Git Hooks and CI

`pre-commit` only runs `git diff --cached --check`. `pre-push` uses Git to check
whitespace in every outgoing commit for each updated ref, including root and
merge commits. Deletion-only pushes need no checks. New refs and unavailable
remote objects exclude commits already known through that remote's tracking
refs. A manual invocation without ref input checks tracked changes against HEAD.
The hook reads committed content during a push, so unrelated working changes do
not affect the result. Neither hook needs npm, .NET, Docker or PowerShell, and
neither hook builds the application or verifies Wiki indexes.

Run focused checks while developing the affected behavior. The full publication
gate lives in `.github/workflows/ci-tests.yml`: Wiki verification, backend build,
formatting and every test project, frontend formatting/linters/dependency baseline,
coverage tests, production/library/Storybook builds, and browser smoke tests.
When changing an integration boundary, run the affected test project locally.
To run the complete backend suite locally:

```bash
dotnet test FoodDiary.slnx --maxcpucount:1
```

For every authorized production release, follow the exact published SHA through
`CI Tests` and its resulting `Deploy` run until both have terminal results.
Successful push, successful CI, and successful deployment are separate facts.
Inspect failed job logs before diagnosing a failure. If a fix is authorized,
follow its replacement SHA through the same chain. Report CI and deployment
links/results and application health separately; a healthy endpoint alone does
not prove which revision was deployed. A superseded or skipped deployment must
not be reported as successful. See
[fooddiary-delivery](../.agents/skills/fooddiary-delivery/SKILL.md).

### Backend CI order and completeness

`backend-fast` builds and runs domain, application, presentation, client and
other unit-only projects. It starts independently of `backend-slow`, whose
matrix has at most four concurrent jobs: `integration-1`, `integration-2`,
`integration-3`, and `mcp`. Each job has its own runner, builds its selected
projects and transitive dependencies once,
and executes its projects sequentially. This bounds Docker resource pressure;
existing xUnit collection isolation remains in effect. Mixed infrastructure
projects run wholly in a slow group, including their unit tests.
The architecture suite runs wholly in `integration-1` because its project-format
convention test launches PowerShell. Its tests remain mandatory and unfiltered.

The three integration groups were balanced using observed CI project durations,
not project counts. `scripts/ci/backend-test-groups.json` is the explicit
partition. `Test-BackendTestGroups.ps1` checks that every solution project with
`Microsoft.NET.Test.Sdk` belongs to exactly one nonempty group and tests rejection
of incomplete/duplicate plans. It also rejects known Docker/PowerShell/slow-test
markers in the fast group. Adding or moving a test project requires updating this
manifest. No category or name filters remove tests from CI. The former PostgreSQL
critical-flow job is superseded by the complete integration projects: both the
Users-owned repository tests and central cleanup tests now run at their actual
paths, without the old empty filter or a duplicate API smoke run.

`backend-format` checks formatting and builds the complete solution independently
of the early tests. The existing `Backend .NET tests and formatting` status is a
final gate over formatting, fast tests, and the entire slow matrix. Failed,
cancelled, or skipped required groups cannot pass it. Deployment still waits for
the successful `CI Tests` workflow; the Wiki job is independent and unchanged by
this test split. Matrix `fail-fast: false` preserves diagnostics from sibling
groups. Slow groups continue after a fast-test failure so all diagnostics remain
available; successful runs no longer pay the fast-group duration before integration
work starts.

Reproduce a group from the repository root with PowerShell 7:

```powershell
./scripts/ci/Test-BackendTestGroups.ps1
./scripts/ci/Invoke-BackendTests.ps1 -Group fast -PlanOnly
./scripts/ci/Invoke-BackendTests.ps1 -Group fast
./scripts/ci/Invoke-BackendTests.ps1 -Group integration-1
./scripts/ci/Invoke-BackendTests.ps1 -Group mcp
```

Use `integration-2` and `integration-3` for the other database/service groups.
Slow integration groups need Docker; MCP needs Node, PowerShell and the frontend
compiler dependencies installed. Group outputs stay under
`.artifacts/backend-ci/<group>/`. Each test project produces a unique TRX file;
zero-test results fail the group. CI uploads TRX, per-project durations and
summaries for seven days, including successful runs. Compare these durations
before rebalancing groups. Test binaries are not transferred between runners,
so each group has some restore/build overhead in exchange for independent paths
and dependencies. Do not run the same group concurrently in one checkout.

## Parallel frontend and Wiki CI

`frontend-lanes` starts four independent runners: `quality-build` retains every
linter, formatting, source check and production/library/Storybook build;
`app-tests` runs the complete app coverage suite; `libraries` runs all nine
time zones and the admin, UI kit, tour and WebMCP coverage suites; `smoke` runs
both browser smoke suites. Each runner installs its own locked dependencies.
The stable `Frontend checks, tests, and builds` status requires the whole matrix
to succeed. Failure, cancellation and skip cannot pass it. Lane-specific log
directories and artifacts preserve simultaneous failures, and the gate assembles
the existing notification artifact without overwriting another lane metadata.

Client smoke uses `fullyParallel` with two workers. Each scenario owns its page,
context and intercepted API data. Parallel execution keeps the same test cases,
assertions, timeouts and zero retries; `@network-audit` retains its existing exclusion.

Wiki CI runs common validation, four focused smoke shards and the three Full
audit shards independently. `Invoke-WikiCiChecks.ps1` retains workspace policy,
portability, session resolution, process cleanup, generated-index, failure-ledger,
change-policy and source-impact checks. Each smoke shard has its own checkout,
graph and temporary files, with at most two child processes. Groups that mutate
the graph retain the existing serial rules within a checkout.

`scripts/ci/wiki-smoke-shards.json` balances the complete `IncludeInAll` catalog
using successful CI group timings. `Test-WikiSmokeShards.ps1` rejects omissions,
duplicates, unknown groups and empty/missing shards, and verifies that the existing
planner selects exactly the declared groups. Adding a focused group requires
updating this partition. All required checks remain enabled on pull requests;
the existing Full-audit skip applies only to pull requests.
The Wiki aggregate requires common checks, the complete focused matrix and every
applicable Full audit. A partial checkout never publishes a full-verification receipt.
Local `wiki.ps1 verify-full` retains its exhaustive focused default.

The initial partition uses group durations from successful CI run
[40455516322](https://github.com/alexindima/food-diary/actions/runs/40455516322)
at `35390c5` (27 October 2026). Each shard contains about 359 seconds of summed
group time from that sample; its wall time also depends on concurrency and cold setup.

Run `node --test scripts/ci/ci-parallelism.test.mjs`,
`./scripts/ci/Test-WikiSmokeShards.ps1` and `./scripts/ci/Test-WikiCiResult.ps1`
for orchestration and failure-gate checks. Successful Wiki shard runs upload group
timings for seven days; compare complete workflow wall time and runner minutes
before/after on GitHub. Local browser timings are not GitHub runner benchmarks.

## Docker readiness for local and AI-assisted tests

Prepare Docker before a direct Docker-dependent `dotnet test` invocation:

```powershell
pwsh -NoProfile -File scripts/Ensure-Docker.ps1
# Run the affected integration test project only after readiness succeeds.
```

The helper requires PowerShell 7 and `docker` on PATH. It probes the selected
Engine with `docker info`, requires Linux containers, and waits up to 180 seconds.
Each CLI probe also has a timeout, so a hung CLI cannot block indefinitely. If a
local Docker Desktop endpoint is selected and unavailable, it requests
`docker desktop start --detach` once and polls until the Engine answers. This
requires the Docker Desktop CLI plugin. It never restarts Docker, switches
contexts or container modes, stops containers, or removes data. SSH/TCP targets
and native Linux sockets are probed without attempting local Desktop startup.
CI and `-NoStart` disable startup while retaining the bounded readiness check.

Use `-TimeoutSeconds 300` for slower machines or `-NoStart` to wait for an Engine
managed elsewhere. A failure reports the readiness deadline, startup outcome and
last Engine diagnostic. Missing CLI and Windows container mode fail immediately.
Check `docker desktop status`, `docker desktop logs` and `docker context show`
when the error persists; a WSL or Desktop failure still needs diagnosis.

`scripts/ci/Invoke-BackendTests.ps1 -Group integration-1` (also `integration-2`
and `integration-3`) performs this check automatically **before restore/build**.
Its controls are `-DockerTimeoutSeconds 300` and `-NoDockerStart`. Fast/MCP groups
and `-PlanOnly` do not invoke Docker. Readiness output is recorded in
`.artifacts/backend-ci/<group>/logs/<group>-docker.log`; failure metadata reports
`FAILED_AREA=docker` and the `docker-readiness` stage.

Always prepare Docker before the test process starts: `DockerAvailability`
caches its result for that process. After a readiness failure, fix the Engine and
start a new test run. Docker-gated skips are not passing integration evidence.
This preparation addresses unavailable or starting Engines; failures inside an
already running PostgreSQL/Redis/RabbitMQ container still need their own diagnosis.

Run `pwsh -NoProfile -File scripts/Test-DockerReadiness.ps1` for deterministic
startup, timeout, endpoint and runner-order regressions. The existing CI backend
partition check runs these fixtures without starting or stopping a real Engine.

## Backend Test Projects

| Project | Purpose |
| --- | --- |
| `Tooling/tests/FoodDiary.ArchitectureTests` | Project references, source conventions, layer boundaries, async/cancellation guardrails. |
| `Modules/<Owner>/tests/FoodDiary.Modules.<Owner>.Application.Tests` | Owner use cases, handlers, validators, mappings and services. |
| `Shared/tests/FoodDiary.Application.Runtime.Tests` | Shared pipeline, transactions and post-commit queue. |
| `Shared/tests/FoodDiary.Application.Contracts.Tests` | Generic validation, pagination, temporal policies and error resolution. |
| `Shared/tests/FoodDiary.Email.Contracts.Tests` | Shared email options. |
| `Modules/BodyMetrics/tests/FoodDiary.Modules.BodyMetrics.Application.Tests` | Weight/waist entry commands, queries, validators, mappings, read services, and date/user-scoping semantics. |
| `Modules/Products/tests/FoodDiary.Modules.Products.Domain.Tests` | Product invariants and all 43 food scoring, grade and unit contract cases from the retired Nutrition suite. |
| `Modules/<Owner>/tests/FoodDiary.Modules.<Owner>.Domain.Tests` | Owner domain invariants, value objects, entities and domain events. |
| `Platform/tests/FoodDiary.Infrastructure.Tests` | Infrastructure unit behavior without external services. |
| `Platform/tests/FoodDiary.Infrastructure.IntegrationTests` | PostgreSQL/Testcontainers persistence and migration behavior. |
| `Tooling/FoodDiary.Testing` | Shared test-only helpers reused by multiple test projects, including Docker-gated test attributes. |
| `Platform/tests/FoodDiary.Presentation.Api.Tests` | Controller flow, HTTP mapping, presentation error behavior. |
| `Hosts/tests/FoodDiary.Web.Api.Tests` | API host options, middleware, health checks, and host service unit behavior. |
| `Hosts/tests/FoodDiary.Web.Api.IntegrationTests` | API host behavior, OpenAPI/Swagger snapshots, HTTP contract snapshots. |
| `Hosts/tests/FoodDiary.JobManager.Tests` | Job registration, recurring job behavior, job execution policy. |
| `Modules/Billing/tests/FoodDiary.Modules.Billing.Application.Tests` | Billing checkout, portal, webhook, renewal and entitlement application behavior. |
| `Modules/Billing/tests/FoodDiary.Modules.Billing.Domain.Tests` | Billing subscription, payment and webhook-event invariants. |
| `Modules/Billing/tests/FoodDiary.Modules.Billing.Infrastructure.Tests` | Billing provider boundary, authenticity and resilience behavior. |
| `Hosts/tests/FoodDiary.Telegram.Bot.Tests` | Bot parsing, command/callback behavior, worker edge cases. |
| `Services/MailRelay/tests/FoodDiary.MailRelay.*.Tests` | MailRelay unit tests split by domain, application, client, infrastructure, initializer, and presentation. |
| `Services/MailRelay/tests/FoodDiary.MailRelay.IntegrationTests` | MailRelay host, PostgreSQL, RabbitMQ, and queue behavior. |
| `Services/MailInbox/tests/FoodDiary.MailInbox.*.Tests` | MailInbox unit tests split by domain, application, client, infrastructure, initializer, and presentation. |
| `Services/MailInbox/tests/FoodDiary.MailInbox.IntegrationTests` | MailInbox PostgreSQL persistence behavior. |
| `Shared/tests/FoodDiary.Mediator.Tests` | Shared mediator behavior. |
| `Shared/tests/FoodDiary.Domain.Primitives.Tests` | Generic domain primitives, guards and value contracts. |
| `Shared/tests/FoodDiary.Results.Tests` | Shared result and error contracts. |
| `Tooling/tests/FoodDiary.Analyzers.Tests` | Build-time analyzer diagnostics and exceptions. |
| `Tooling/tests/FoodDiary.Development.Mcp.Tests` | Development MCP protocol, context retrieval and process behavior. |

Shared and Tooling test projects reuse `Tooling/Testing/TestProjects.props` and its
runsettings/runner files. Their solution folders match their physical owner:
`/Shared/tests/` and `/Tooling/tests/`. This is test organization, not a new runtime
boundary; production projects and assembly identities are unchanged. General
architecture, host, cross-module and test-support projects remain under `tests/`.

## Frontend Checks

Run from `FoodDiary.Web.Client`.

| Command | Purpose |
| --- | --- |
| `npm run lint` | ESLint rules, Angular rules, import boundaries, accessibility, local custom rules. |
| `npm run lint:deps:strict` | Dependency Cruiser graph boundaries. |
| `npm run stylelint` | CSS/SCSS rules and ordering. |
| `npm run build` | Main Angular build. |
| `npm run build:admin` | Admin app build. |
| `npm run test:ci:app` | Main app unit tests. |
| `npm run test:ci:ui-kit` | UI kit unit tests. |
| `npm run test:ci:admin` | Admin app unit tests. |
| `npm run check:i18n` | Locale consistency. |
| `npm run check:seo-prerender` | SEO prerender HTML checks. |
| `npm run test:performance-build` | Startup/service-worker graph and deterministic public Lighthouse runner checks. |
| `npm run audit:lighthouse` | Opt-in production build and repeated public-page mobile/desktop audits. See [public Lighthouse workflow](frontend/PUBLIC_LIGHTHOUSE.md). |
| `npm run audit:lighthouse:check` | Enforce saved Lighthouse budgets and baseline regression tolerances without launching Chrome. |
| `npm run verify` | Full frontend verification chain. |

User and Admin coverage excludes only their generated SDK directories. Feature
adapters, response mappers, transport helpers, and other handwritten code remain
in coverage with the existing thresholds. Generated clients are verified by the
SDK generation and live API contract gates, alongside adapter HTTP tests.

## Contract Snapshots

If backend HTTP routes, payloads, status codes, OpenAPI output, or Swagger-visible behavior changes intentionally:
- update snapshots under `Hosts/tests/FoodDiary.Web.Api.IntegrationTests/Snapshots/`,
- include snapshot changes in the same commit,
- mention the contract change in the PR/commit summary.

Presentation or host changes may require both presentation tests and integration snapshots.

CI compares API contracts with the last successful ancestral CI push. Failed
runs do not advance that baseline. An explicitly approved coordinated client
upgrade may record its exact transition in
`.llm-wiki/policies/api-compatibility-acceptance.json`: the base commit, before/after
fingerprints of both contract snapshots, and complete accepted change identities.
The guard still reports all breaking changes and fails on every unaccepted one;
changed fingerprints, another baseline or stale identities cannot reuse approval.
Synthetic comparisons do not load the record. Update all supported clients with
the API for an accepted transition. See the Wiki API compatibility workflow for
the release-specific decision and rollout obligations.

## Migration Tests And Safety

For EF Core migrations:
- commit both `*.cs` and `*.Designer.cs`,
- run a whitespace/style pass on migration files,
- prefer `dotnet format whitespace FoodDiary.Infrastructure/FoodDiary.Infrastructure.csproj`,
- avoid rollback/reapply guidance for shared or production databases unless it is explicitly operationally safe.

## Choosing What To Run

For a narrow docs-only change:
- `git diff --check`

For architecture guide/test changes:
- `dotnet test Tooling/tests/FoodDiary.ArchitectureTests/FoodDiary.ArchitectureTests.csproj`

For primary backend application changes:
- relevant project tests,
- architecture tests if dependencies or folders changed,
- API integration tests if HTTP behavior changed.

For frontend feature changes:
- `npm run lint`,
- relevant `npm run test:ci:*`,
- `npm run check:i18n` if copy changed,
- build command for the affected app.

Before release or large PR:
- `dotnet build FoodDiary.slnx`,
- `cd FoodDiary.Web.Client && npm run verify`.

## Dashboard logical extraction

Dashboard owns Application, Application.Abstractions, Contracts and HTTP presentation
projects, with no Domain, PersistenceModel or Infrastructure project.
AddDashboardModule registers the scoped statistics adapter over Meals contracts;
host AddReadModelComposition supplies SQL body/meal readers. Statistics and WeeklyCheckIn
dispatch ReadMealNutritionStatisticsQuery from Meals directly. Dashboard bucket DTOs
remain local to the snapshot contract. Cross-module EF projection tests live in
Platform Infrastructure.Tests; snapshot application tests stay with Dashboard.
Preserve scoped identity, filtering, ordering and the single weekly read for one-day
snapshots. See ADR 0049.

Users-focused application tests live in `Modules/Users/tests/FoodDiary.Modules.Users.Application.Tests`. Mixed Authentication/Admin/application tests remain in the central donor suite, while shared repository, cleanup, EF model, provider-backed PostgreSQL, host composition, presentation, and HTTP contract tests remain with their established cross-module owners. Users extraction verification must include the focused module suite, central application/Identity consumers, full architecture tests, provider-backed Infrastructure integration tests, HTTP integration tests, EF pending-model detection, and NuGet vulnerability audit; it must not add a migration when the model is unchanged.

Identity-focused Authentication tests live in `Modules/Identity/tests/FoodDiary.Modules.Identity.Application.Tests`. The Users-owned authentication-registration service test and mixed Admin/email-template/login-activity tests remain in the central donor suite. Persistence, provider-backed PostgreSQL, host composition, presentation, and HTTP contract tests remain with their established owners; the module test assembly preserves the legacy friend-assembly name used by internal application services.

## Recipes physical ownership

Recipes use cases, ports, read contracts, persistence model and adapters live under `Modules/Recipes`. Recipe/Steps/Ingredients and values/events belong to Recipes Domain; IDs belong to Recipes Domain.Contracts. Foreign relationships use scalar IDs and immutable read snapshots. Shared context/migrations/snapshot and cross-module tests stay central. Hosts compose AddRecipesModule; JobManager uses AddRecipesPersistence without adding application handlers. See `docs/ai/recipes-ownership-inventory.md`; this is not full Domain/database isolation.

## Admin physical ownership

Admin owns application slices, billing-report/impersonation/mail-reader ports,
AdminImpersonationSession Domain, its explicit EF model and reporting/session
adapters under Modules/Admin. CLR namespaces follow their owning projects;
compatibility requires coordinated host rebuilds. Email templates
remain Identity-owned and role audit/User capabilities remain Users-owned. Shared context/migrations/cleanup, SSO store/JWT providers,
HTTP authorization, structured audit and MailInbox client bridge remain central.
Hosts call AddAdminModule; JobManager adds only AddAdminPersistence. See
docs/ai/admin-ownership-inventory.md for current source evidence and test ownership.

## Products physical ownership

Products aggregate/value-object invariants, application behavior and focused
persistence tests live under `Modules/Products`. Central mixed-domain, HTTP and
full PostgreSQL suites verify unidirectional mappings and legacy snapshots. Hosts explicitly
compose AddProductsModule; JobManager adds AddProductsPersistence without new
handlers. See `docs/ai/products-ownership-inventory.md` for the boundary and
coordinated-rebuild compatibility promise.

## Meals physical ownership

Meals-only application tests, Meal and MealAI invariant tests, and the real-PostgreSQL
MealRepository suite live under `Modules/Meals/tests` in separate projects. Mixed
central Domain/DI suites and cross-module Export, Favorites, Gamification, USDA,
WeeklyGoals, WeeklyCheckIn, Dashboard, Presentation and Web API suites remain with
their existing owners. Do not duplicate them in Meals. The provider-backed suite
must remain unfiltered in extraction validation so the moved repository exercises
the shared DbContext and central User/Product/Recipe relationship seams.

## Users Domain ownership

Users owns the complete User aggregate, all credential/security partials, roles,
role audit and weight/waist goals under `Modules/Users/Domain`. `UserId` lives in
`Modules/Users/Domain.Contracts`, depending only on shared primitives. Consumers
reference the exact owner; central Domain retains shared guards and values without
an aggregate re-export. Authentication flows/providers, combined UserRepository,
DbContext, migrations and snapshot retain their existing owners. CLR namespaces,
security behavior and EF/HTTP contracts are unchanged. See
`docs/ai/users-domain-extraction.md` for residual seams and verification evidence.

## Contract and aggregate isolation

ADR 0031 completes the contract-cycle and foreign-navigation follow-up to ADR 0030.
The combined Application/service-contract graph is acyclic. Foreign domain links are
scalar IDs; immutable snapshots and no-tracking joins supply display/nutrition data.
Meals owns nutrition aggregation. FD0015/FD0016 enforce module EF ownership and exact
reviewed technical escapes during compilation. The database, FK behavior and public
API remain shared/compatible. See `docs/adr/0031-acyclic-contracts-and-scalar-aggregate-links.md`.

Retry and image-reference boundaries follow [ADR 0032](adr/0032-retry-isolation-and-image-reference-integrity.md): fresh attempt state, isolated orphan cleanup, restrictive image FKs and immutable Images service contracts.

See [ADR 0033](adr/0033-retry-safe-outbox-and-consumer-contracts.md) for retry-safe Wearables/replay, outbox claim-owner fencing and the narrow cross-module consumer APIs.

## Dashboard regression coverage

`node scripts/ai/verify-user-journeys.mjs` adds separate live persistence journeys
against an owned local API, PostgreSQL and frontend proxy. It type-checks generated
SDK request fixtures, verifies hydration across dashboard reload, and verifies
meal quantity/nutrition across save/read/reopen. The existing mocked browser smoke
remains the fast rendered-state suite. See `docs/ai/AI_DEVELOPMENT.md` for lifecycle,
provider limits, FsCheck nutrition properties and the scoped mutation pilot.

The dashboard page spec keeps its real template and tests bindings/composition with
isolated service and child boundaries. Card specs exercise rendered controls;
facade specs cover loading races, refresh failures, favorites and writes. Weight
and waist share the same state, locale and target-label regression cases.

Browser cases named `dashboard regression` in
`FoodDiary.Web.Client/e2e/client-smoke/client-smoke.spec.ts` run as part of the
existing client smoke CI job. They intercept API requests, use synthetic data,
and do not mutate a real user's diary. They cover:

- Fasting in intermittent, extended and cyclic modes at 360, 390 and 1440 px,
  with non-overlapping current-time and elapsed-time labels.
- Today's mobile hydration placement and historical-day quick-action restrictions.
- Weekly chart selection, empty/populated detail geometry, opening a date and browser back.
- Weight/waist chart alignment and keyboard navigation; the weight goal below the plot.
- Hydration writes and refresh, calorie-goal application without bubbling into the detail dialog,
  and editing a layout without accidentally navigating or saving.

Run from `FoodDiary.Web.Client`:

```powershell
npm run test:ci:app
npx playwright test --config playwright.client.smoke.config.ts --grep "dashboard regression"
```

The browser checks assert layout relationships rather than OS-dependent pixel
snapshots. They are not a pixel-perfect visual baseline, a backend integration
suite, or a guarantee of exhaustive coverage. Keep manual checks for typography,
colour, photos and long translated content. Use an advancing controlled clock:
freezing `Date.now()` prevents Angular deferred views with a minimum loading time
from completing.


### Frontend coverage and state isolation

CI runs `test:coverage:app`, `test:coverage:tour`, `test:coverage:webmcp`,
`test:coverage:admin`, and `test:coverage:ui-kit`. `npm run test:coverage` runs the
same five projects locally.
Each project's `coverage` configuration in `FoodDiary.Web.Client/angular.json`
explicitly includes its own TypeScript source tree, including files that no test
imports. Specs, declarations, and Storybook stories are excluded; runtime facades
and services are not. Angular HTML templates and Playwright execution are not
included in these TypeScript coverage figures.

`vitest.config.mjs` keeps reports even when tests fail. HTML, JSON,
JSON-summary, and lcov reports live in `FoodDiary.Web.Client/coverage/<project>/`.
CI uploads this directory with `if: always()` as `frontend-checks-coverage-app-tests` and `frontend-checks-coverage-libraries`.
Open `index.html` for line/branch details; consult functions as well as branches:
an uncalled error callback can coexist with 100% branch coverage.

The main app has minimum coverage thresholds in its Angular configuration
(80% lines, 81% statements, 75% branches, 78% functions). Key dashboard, fasting,
weekly check-in, and user API files also have individual thresholds in
`vitest.config.mjs`, so their coverage cannot be hidden by unrelated tests. Raise
these as important gaps close; do not lower them or exclude runtime files to make
a build pass. A passing percentage does not replace assertions about outcomes,
request payloads, failure recovery, cancellation, or stale responses. Component
tests using facade mocks protect presentation but do not test facade behavior.

The main app enables test-file isolation, with four workers configured in
`vitest.config.mjs` to bound resource usage in local runs and CI.
`src/test-setup.ts` clears local and session storage on both the test global and
the jsdom window before and after every test. Seed storage in each test or its
`beforeEach`, never rely on another test's persisted state. Keep time-dependent
facade tests deterministic and restore fake timers after each test.

The retry interceptor, product-list URL adapter, Premium checkout page, and Admin
audit/moderation API adapters have file-specific floors too. Their regressions
cover retry timing/exhaustion/cancellation, real-router query ownership, delayed
entitlement activation and Paddle return failures, and exact administrative HTTP
contracts including invalid responses and rejected mutations.

Admin and all three libraries also enforce project-wide minimums in their Angular
coverage configurations. These floors preserve the measured baseline; they do
not imply that every important scenario has a test. In lines/statements/branches/
functions order, the minimums are Admin 75/76/72/67, UI kit 85/86/84/87, tour
75/76/69/73, and WebMCP 96/96/96/95. Raise the relevant floor after closing a
meaningful gap rather than compensating for a regression in another project.

Dashboard facade regressions include meal mutations/favorites, initial and silent
load failures, language/day changes, stale responses, and destruction. Water tests
use separate asynchronous write and refresh responses: controls stay busy across
both, and duplicate writes are ignored. Weight and waist API goal tests share the
same success/error contract checks. Weekly check-in tests use actual Angular
resources to verify retained data during refresh and obsolete-week results.
