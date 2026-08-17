# FoodDiary

FoodDiary is a food tracking platform with a .NET backend, Angular frontend, Telegram bot, and background jobs.

## Repository Structure

- `FoodDiary.Domain` - domain model, aggregates, value objects, domain events
- `FoodDiary.Application.Abstractions` - application-facing ports and shared models
- `FoodDiary.Application.Runtime` - mediator pipeline, transactions, and post-commit runtime behavior
- `FoodDiary.Application.<Feature>` - independently compiled feature use cases, services, and validation
- `FoodDiary.Infrastructure` - EF Core, PostgreSQL, auth, external providers, persistence adapters
- `FoodDiary.Integrations` - external provider adapters and supporting service clients
- `FoodDiary.Presentation.Api` - HTTP transport layer, controllers, request/response mappings
- `FoodDiary.Web.Api` - ASP.NET Core host and composition root
- `FoodDiary.JobManager` - background jobs host
- `FoodDiary.MailRelay.*` - internal outbound email relay, queue processor, client package, and initializer
- `FoodDiary.MailInbox.*` - inbound email service, client package, and initializer
- `FoodDiary.Telegram.Bot` - Telegram adapter/worker
- `FoodDiary.Web.Client` - Angular web client, admin frontend, and UI kit
- `Shared/FoodDiary.Mediator` - lightweight shared mediator
- `tests` - application, architecture, infrastructure, API, bot, jobs, and integration tests
- `docs` - architecture, testing, backend governance, frontend architecture, ADRs, and active plans

## Backend Architecture

The backend follows a layered architecture with explicit dependency direction:

1. `Domain` knows nothing about outer layers.
2. Feature application modules depend on `Application.Abstractions`, `Domain`, and the shared mediator as needed.
3. `Application.Runtime` owns cross-cutting mediator execution without aggregating feature modules.
4. `Infrastructure` implements application-facing abstractions.
5. `Presentation.Api` maps transport contracts to feature commands and queries.
6. `Web.Api` is the host that explicitly composes presentation, runtime, feature modules, infrastructure, and integrations.

These constraints are enforced by architecture tests in `tests/FoodDiary.ArchitectureTests`.

More detail:

- `docs/ARCHITECTURE.md`
- `docs/BACKEND_MODULE_MAP.md`
- `docs/adr/`

## Main Flows

- Web client calls the ASP.NET API.
- `Presentation.Api` maps HTTP requests to application commands and queries.
- The owning `Application.<Feature>` module executes each use case against domain models and abstractions.
- `Infrastructure` provides persistence and external integrations.
- `JobManager` runs scheduled maintenance workflows.
- `Telegram.Bot` handles Telegram-specific interaction as a separate adapter.

## Build

Backend:

```bash
dotnet build FoodDiary.slnx
```

Shared MSBuild settings keep only the SkiaSharp native assets required by the target runtime and omit native PDB files from build output. Cross-platform publishes must therefore specify the deployment RID (for example, `linux-x64`).

Frontend:

```bash
cd FoodDiary.Web.Client
npm install
npm run build
```

## Test

Backend tests:

```bash
dotnet test FoodDiary.slnx --maxcpucount:1
```

Architecture guardrails:

```bash
dotnet test tests/FoodDiary.ArchitectureTests/FoodDiary.ArchitectureTests.csproj
```

Backend verification:

```bash
dotnet restore FoodDiary.slnx
dotnet format FoodDiary.slnx --verify-no-changes --no-restore
dotnet build FoodDiary.slnx --configuration Release --no-restore
dotnet test FoodDiary.slnx --configuration Release --no-restore --no-build --maxcpucount:1
```

Frontend checks:

```bash
cd FoodDiary.Web.Client
npm run verify
```

## Configuration

Repository-tracked `appsettings*.json` files should contain only safe bootstrap values.

- Keep real secrets out of git.
- Use `.NET user-secrets`, environment variables, or deployment secret stores for real values.
- Treat tracked connection strings and JWT secrets as placeholders only.
- Override environment-specific public URLs outside the repository when possible.

Minimum local setup:

```bash
cd FoodDiary.Web.Api
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Database=fooddiary;Username=postgres;Password=your-local-password"
dotnet user-secrets set "Jwt:SecretKey" "your-very-long-local-development-secret"
```

## Backend Quality Gates

Backend work should follow these repository documents:

- `docs/backend/BACKEND_TIME_POLICY.md`
- `docs/backend/BACKEND_CRITICAL_FLOW_MATRIX.md`
- `docs/backend/BACKEND_DEFINITION_OF_DONE.md`
- `docs/backend/BACKEND_API_CONTRACT_GOVERNANCE.md`
- `docs/backend/BACKEND_OBSERVABILITY_BASELINE.md`
- `docs/backend/BACKEND_PERFORMANCE_REVIEW.md`
- `docs/backend/BACKEND_MIGRATION_SAFETY.md`
- `docs/backend/BACKEND_SECURITY_HARDENING.md`
- `docs/backend/BACKEND_RUNBOOKS.md`

Frontend architecture and observability:

- `docs/frontend/FRONTEND_ARCHITECTURE.md`
- `docs/frontend/FRONTEND_OBSERVABILITY_BASELINE.md`

Testing strategy:

- `docs/TESTING_STRATEGY.md`

## Deployment Notes

- CI runs .NET validation and Telegram failure notifications.
- Deploy is performed through GitHub Actions.
- Deploy copies `docker-compose.yml` to `/opt/fooddiary`, uses `/etc/fooddiary/fooddiary.env` as the server env source, runs migrations through `db-init`, starts `api` and `telegram-bot`, and publishes client static files from the `client` image.
- Host-level observability configuration such as the current `promtail` baseline lives in `infra/observability/`.
- Backend and deploy recovery steps are documented in `docs/backend/BACKEND_RUNBOOKS.md`.
- PR review discipline expects the security/release checklist from `docs/backend/BACKEND_SECURITY_HARDENING.md` to be reflected in the PR template for security-relevant changes.

## Status

The repository is in active development. Prefer project-specific guides from `AGENTS.md` when working in a specific folder.
