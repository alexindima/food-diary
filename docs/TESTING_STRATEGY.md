# Testing Strategy

Notifications-focused application, aggregate and provider/persistence tests live under `Modules/Notifications/tests`. Central projects retain HTTP/host, shared DbContext/Postgres and cross-module DI coverage.

## Fast Architecture Feedback
Run architecture tests when changing project references, folders, boundary rules, controllers, async method conventions, or service client packages.

```bash
dotnet test tests/FoodDiary.ArchitectureTests/FoodDiary.ArchitectureTests.csproj
```

These tests are also the best executable documentation for backend boundaries.
They also guard the allowed reference graph between test projects, so shared test helpers should be added through `tests/FoodDiary.Testing` instead of ad hoc cross-test-project references.

## Backend Test Projects

| Project | Purpose |
| --- | --- |
| `tests/FoodDiary.ArchitectureTests` | Project references, source conventions, layer boundaries, async/cancellation guardrails. |
| `tests/FoodDiary.Application.Tests` | Application use cases, handlers, validation, application services. |
| `Modules/BodyMetrics/tests/FoodDiary.Modules.BodyMetrics.Application.Tests` | Weight/waist entry commands, queries, validators, mappings, read services, and date/user-scoping semantics. |
| `Modules/Products/tests/FoodDiary.Modules.Products.Domain.Tests` | Product invariants and all 43 food scoring, grade and unit contract cases from the retired Nutrition suite. |
| `tests/FoodDiary.Domain.Tests` | Core domain invariants, value objects, entities, and domain events. |
| `tests/FoodDiary.Infrastructure.Tests` | Infrastructure unit behavior without external services. |
| `tests/FoodDiary.Infrastructure.IntegrationTests` | PostgreSQL/Testcontainers persistence and migration behavior. |
| `tests/FoodDiary.Testing` | Shared test-only helpers reused by multiple test projects, including Docker-gated test attributes. |
| `tests/FoodDiary.Presentation.Api.Tests` | Controller flow, HTTP mapping, presentation error behavior. |
| `tests/FoodDiary.Web.Api.Tests` | API host options, middleware, health checks, and host service unit behavior. |
| `tests/FoodDiary.Web.Api.IntegrationTests` | API host behavior, OpenAPI/Swagger snapshots, HTTP contract snapshots. |
| `tests/FoodDiary.JobManager.Tests` | Job registration, recurring job behavior, job execution policy. |
| `Modules/Billing/tests/FoodDiary.Modules.Billing.Application.Tests` | Billing checkout, portal, webhook, renewal and entitlement application behavior. |
| `Modules/Billing/tests/FoodDiary.Modules.Billing.Domain.Tests` | Billing subscription, payment and webhook-event invariants. |
| `Modules/Billing/tests/FoodDiary.Modules.Billing.Infrastructure.Tests` | Billing provider boundary, authenticity and resilience behavior. |
| `tests/FoodDiary.Telegram.Bot.Tests` | Bot parsing, command/callback behavior, worker edge cases. |
| `Services/MailRelay/tests/FoodDiary.MailRelay.*.Tests` | MailRelay unit tests split by domain, application, client, infrastructure, initializer, and presentation. |
| `Services/MailRelay/tests/FoodDiary.MailRelay.IntegrationTests` | MailRelay host, PostgreSQL, RabbitMQ, and queue behavior. |
| `Services/MailInbox/tests/FoodDiary.MailInbox.*.Tests` | MailInbox unit tests split by domain, application, client, infrastructure, initializer, and presentation. |
| `Services/MailInbox/tests/FoodDiary.MailInbox.IntegrationTests` | MailInbox PostgreSQL persistence behavior. |
| `Shared/tests/FoodDiary.Mediator.Tests` | Shared mediator behavior. |
| `Shared/tests/FoodDiary.Domain.Primitives.Tests` | Generic domain primitives, guards and value contracts. |
| `Shared/tests/FoodDiary.Results.Tests` | Shared result and error contracts. |
| `Tooling/tests/FoodDiary.Analyzers.Tests` | Build-time analyzer diagnostics and exceptions. |
| `Tooling/tests/FoodDiary.Development.Mcp.Tests` | Development MCP protocol, context retrieval and process behavior. |

Shared and Tooling test projects reuse `tests/Directory.Build.props` and its
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
| `npm run verify` | Full frontend verification chain. |

## Contract Snapshots

If backend HTTP routes, payloads, status codes, OpenAPI output, or Swagger-visible behavior changes intentionally:
- update snapshots under `tests/FoodDiary.Web.Api.IntegrationTests/Snapshots/`,
- include snapshot changes in the same commit,
- mention the contract change in the PR/commit summary.

Presentation or host changes may require both presentation tests and integration snapshots.

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
- `dotnet test tests/FoodDiary.ArchitectureTests/FoodDiary.ArchitectureTests.csproj`

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

`Modules/Dashboard` owns Application, Application/Abstractions, Contracts and
Infrastructure. It is a read composer with no Domain or PersistenceModel. Stable
statistics contracts retain their CLR namespaces and are referenced one-way by
central Abstractions for Statistics/Cycles/WeeklyCheckIn/Tdee/Gamification consumers.
Optimized projection readers own no contributing aggregates. Shared DbContext,
migrations/model snapshot and HTTP transport remain central. Hosts explicitly call
`AddDashboardReadServices` after infrastructure registration; Application fallback
registration remains `AddDashboardModule`. Scoped concrete/interface aliases and
query behavior are preserved. Owned application/adapter tests live under module
tests; mixed DI/date, shared PostgreSQL and HTTP suites remain central.

Users-focused application tests live in `Modules/Users/tests/FoodDiary.Modules.Users.Application.Tests`. Mixed Authentication/Admin/application tests remain in the central donor suite, while shared repository, cleanup, EF model, provider-backed PostgreSQL, host composition, presentation, and HTTP contract tests remain with their established cross-module owners. Users extraction verification must include the focused module suite, central application/Identity consumers, full architecture tests, provider-backed Infrastructure integration tests, HTTP integration tests, EF pending-model detection, and NuGet vulnerability audit; it must not add a migration when the model is unchanged.

Identity-focused Authentication tests live in `Modules/Identity/tests/FoodDiary.Modules.Identity.Application.Tests`. The Users-owned authentication-registration service test and mixed Admin/email-template/login-activity tests remain in the central donor suite. Persistence, provider-backed PostgreSQL, host composition, presentation, and HTTP contract tests remain with their established owners; the module test assembly preserves the legacy friend-assembly name used by internal application services.

## Recipes physical ownership

Recipes use cases, ports, read contracts, persistence model and adapters live under `Modules/Recipes`. Recipe/Steps/Ingredients and values/events belong to Recipes Domain; IDs belong to Recipes Domain.Contracts. Foreign relationships use scalar IDs and immutable read snapshots. Shared context/migrations/snapshot and cross-module tests stay central. Hosts compose AddRecipesModule; JobManager uses AddRecipesPersistence without adding application handlers. See `docs/ai/recipes-ownership-inventory.md`; this is not full Domain/database isolation.

## Admin physical ownership

Admin owns application slices, billing-report/impersonation/mail-reader ports,
AdminImpersonationSession Domain, its explicit EF model and reporting/session
adapters under Modules/Admin. Legacy application assembly and CLR namespaces
remain stable; compatibility requires coordinated host rebuilds. Email templates
remain Identity-owned and role audit/User capabilities remain Users-owned despite
legacy Admin namespaces. Shared context/migrations/cleanup, SSO store/JWT providers,
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
