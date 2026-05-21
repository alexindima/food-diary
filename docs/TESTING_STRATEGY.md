# Testing Strategy

## Fast Architecture Feedback
Run architecture tests when changing project references, folders, boundary rules, controllers, async method conventions, or service client packages.

```bash
dotnet test tests/FoodDiary.ArchitectureTests/FoodDiary.ArchitectureTests.csproj
```

These tests are also the best executable documentation for backend boundaries.

## Backend Test Projects

| Project | Purpose |
| --- | --- |
| `tests/FoodDiary.ArchitectureTests` | Project references, source conventions, layer boundaries, async/cancellation guardrails. |
| `tests/FoodDiary.Application.Tests` | Application use cases, handlers, validation, application services. |
| `tests/FoodDiary.Infrastructure.Tests` | EF Core, persistence, infrastructure implementations, initializer/integration wiring where needed. |
| `tests/FoodDiary.Presentation.Api.Tests` | Controller flow, HTTP mapping, presentation error behavior. |
| `tests/FoodDiary.Web.Api.IntegrationTests` | API host behavior, OpenAPI/Swagger snapshots, HTTP contract snapshots. |
| `tests/FoodDiary.JobManager.Tests` | Job registration, recurring job behavior, job execution policy. |
| `tests/FoodDiary.Telegram.Bot.Tests` | Bot parsing, command/callback behavior, worker edge cases. |
| `tests/FoodDiary.MailRelay.Tests` | MailRelay application/client/host behavior. |
| `tests/FoodDiary.MailInbox.Tests` | MailInbox application/infrastructure/presentation behavior. |
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
