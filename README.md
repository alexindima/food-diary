# FoodDiary

FoodDiary is a food tracking platform with a .NET backend, Angular frontend, Telegram bot, and background jobs.

## Repository Structure

- `FoodDiary.Domain` - domain model, aggregates, value objects, domain events
- `FoodDiary.Application` - use cases, application services, ports, validation
- `FoodDiary.Infrastructure` - EF Core, PostgreSQL, auth, external providers, persistence adapters
- `FoodDiary.Presentation.Api` - HTTP transport layer, controllers, request/response mappings
- `FoodDiary.Web.Api` - ASP.NET Core host and composition root
- `FoodDiary.JobManager` - background jobs host
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

## Deployment Notes

- CI runs .NET validation and Telegram failure notifications.
- Deploy is performed through GitHub Actions.
- Database migrations are bundled and executed during deploy.

## Status

The repository is in an active migration/cleanup phase. Prefer project-specific guides from `AGENTS.md` when working in a specific folder.
