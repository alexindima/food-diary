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
| `MailRelay/tests/FoodDiary.MailRelay.*.Tests` | MailRelay unit tests split by domain, application, client, infrastructure, initializer, and presentation. |
| `MailRelay/tests/FoodDiary.MailRelay.IntegrationTests` | MailRelay host, PostgreSQL, RabbitMQ, and queue behavior. |
| `MailInbox/tests/FoodDiary.MailInbox.*.Tests` | MailInbox unit tests split by domain, application, client, infrastructure, initializer, and presentation. |
| `MailInbox/tests/FoodDiary.MailInbox.IntegrationTests` | MailInbox PostgreSQL persistence behavior. |
| `tests/FoodDiary.Mediator.Tests` | Shared mediator behavior. |

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

## Recipes physical ownership

Recipes use cases, ports, read contracts, persistence model and adapters live under `Modules/Recipes`. Recipe/Steps/Ingredients, IDs/value objects/events remain central Domain because public User/MealItem/Product inverse navigations prohibit a one-way extraction. Shared context/migrations/snapshot and cross-module tests stay central. Hosts compose AddRecipesModule; JobManager uses AddRecipesPersistence without adding application handlers. See `docs/ai/recipes-ownership-inventory.md`; this is not full Domain/database isolation.
