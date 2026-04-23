# FoodDiary

FoodDiary is a food tracking platform with a .NET backend, Angular frontend, Telegram bot, and background jobs.

## Repository Structure

- `FoodDiary.Domain` - domain model, aggregates, value objects, domain events
- `FoodDiary.Application` - use cases, application services, ports, validation
- `FoodDiary.Infrastructure` - EF Core, PostgreSQL, auth, external providers, persistence adapters
- `FoodDiary.Presentation.Api` - HTTP transport layer, controllers, request/response mappings
- `FoodDiary.Web.Api` - ASP.NET Core host and composition root
- `FoodDiary.JobManager` - background jobs host
- `FoodDiary.MailRelay` - internal outbound email relay and queue processor
- `FoodDiary.Telegram.Bot` - Telegram adapter/worker
- `FoodDiary.Web.Client` - Angular web client and admin frontend
- `tests` - application, architecture, infrastructure, API, bot, jobs, and integration tests

## Backend Architecture

The backend follows a layered architecture with explicit dependency direction:

1. `Domain` knows nothing about outer layers.
2. `Application` depends on `Domain`.
3. `Infrastructure` depends on `Application`.
4. `Presentation.Api` depends on `Application`.
5. `Web.Api` is the host that wires `Presentation.Api` and `Infrastructure` together.

These constraints are enforced by architecture tests in `tests/FoodDiary.ArchitectureTests`.

## Main Flows

- Web client calls the ASP.NET API.
- `Presentation.Api` maps HTTP requests to application commands and queries.
- `Application` executes use cases against domain model and abstractions.
- `Infrastructure` provides persistence and external integrations.
- `JobManager` runs scheduled maintenance workflows.
- `Telegram.Bot` handles Telegram-specific interaction as a separate adapter.

## Build

Backend:

```bash
dotnet build FoodDiary.slnx
```

Frontend:

```bash
cd FoodDiary.Web.Client
npm install
npm run build
```

## Test

Backend tests:

```bash
dotnet test FoodDiary.slnx
```

Frontend checks:

```bash
cd FoodDiary.Web.Client
npm run lint
npm run stylelint
npm run build:prod
npm run build:admin
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

- `BACKEND_TIME_POLICY.md`
- `BACKEND_CRITICAL_FLOW_MATRIX.md`
- `BACKEND_DEFINITION_OF_DONE.md`
- `BACKEND_API_CONTRACT_GOVERNANCE.md`
- `BACKEND_OBSERVABILITY_BASELINE.md`
- `BACKEND_PERFORMANCE_REVIEW.md`
- `BACKEND_MIGRATION_SAFETY.md`
- `BACKEND_SECURITY_HARDENING.md`
- `BACKEND_RUNBOOKS.md`
- `FRONTEND_OBSERVABILITY_BASELINE.md`

## Deployment Notes

- CI runs .NET validation and Telegram failure notifications.
- Deploy is performed through GitHub Actions.
- Deploy copies `docker-compose.yml` to `/opt/fooddiary`, uses `/etc/fooddiary/fooddiary.env` as the server env source, runs migrations through `db-init`, starts `api` and `telegram-bot`, and publishes client static files from the `client` image.
- Host-level observability configuration such as the current `promtail` baseline lives in `infra/observability/`.
- Backend and deploy recovery steps are documented in `BACKEND_RUNBOOKS.md`.
- PR review discipline now expects the security/release checklist from `BACKEND_SECURITY_HARDENING.md` to be reflected in the PR template for security-relevant changes.

## Status

The repository is in active development. Prefer project-specific guides from `AGENTS.md` when working in a specific folder.
