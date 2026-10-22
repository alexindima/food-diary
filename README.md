# FoodDiary

FoodDiary is a food tracking platform with an Angular web client, a .NET API, a Telegram bot, and background jobs. The primary backend is a modular monolith; supporting mail and bug triage services have separate hosts.

Start with the [documentation index](docs/README.md), [architecture](docs/ARCHITECTURE.md), and [backend module map](docs/BACKEND_MODULE_MAP.md). Repository contribution rules live in [AGENTS.md](AGENTS.md); read the nearest project-specific guide before changing code.

## Requirements

- .NET 10 SDK selected by [global.json](global.json) (`10.0.102`, with patch roll-forward).
- Node.js 22 and npm, matching the CI toolchain.
- PostgreSQL 17 for local application data.
- PowerShell 7 (`pwsh`) for repository scripts and the local setup examples below.
- Docker for Testcontainers integration tests and container-based services.

## Repository Structure

| Path | Responsibility |
| --- | --- |
| `Modules/<Feature>/` | Business modules with their own application, domain, contracts, persistence, infrastructure, presentation, and tests as needed. |
| `Shared/` | Narrow shared contracts, domain primitives, mediator, application/persistence runtimes, and technical adapters. |
| `Services/` | Independent MailRelay, MailInbox, and BugTriage services, including their tests and deployment support. |
| `FoodDiary.Web.Api/` | ASP.NET Core host and composition root. |
| `FoodDiary.Presentation.Api/` | Shared HTTP/SignalR transport primitives, filters, error mapping, and version endpoint. |
| `FoodDiary.Infrastructure/` | Central EF Core migration model, migrations, and persistence composition. |
| `FoodDiary.ReadModel.Composition/` | Explicit cross-module SQL read projections. |
| `FoodDiary.Initializer/` | Database migration, seed, and operational console host. |
| `FoodDiary.JobManager/`, `FoodDiary.Telegram.Bot/` | Background execution and Telegram adapter hosts. |
| `FoodDiary.Web.Client/` | Angular user and admin apps, UI kit, tour engine, WebMCP library, and frontend tests. |
| `Hosts/tests/`, `Platform/tests/`, `Shared/tests/`, `Tooling/tests/` | Host, platform, shared-component, analyzer, and architecture tests; module/service tests stay with their owners. |
| `docs/`, `infra/`, `scripts/`, `.github/workflows/` | Documentation, infrastructure configuration, repository tooling, and CI/deployment workflows. |

Additional entrypoints: [mobile shell](FoodDiary.Mobile/README.md), [technology radar](FoodDiary.TechRadar/README.md), and [development MCP server](FoodDiary.Development.Mcp/README.md).

## Backend Architecture

- Business rules and write ownership belong to the owning module under `Modules/<Feature>`. Cross-module operations use explicit owner contracts.
- Each module's `Presentation` project maps HTTP requests to application commands and queries. Reusable wire DTOs and pure response mappings live in owner `Presentation.Contracts` and `Presentation.Mappings` projects.
- `FoodDiary.Presentation.Api` supplies the shared transport kernel. Executable hosts explicitly compose the modules and technical services they need.
- `Shared/FoodDiary.Application.Runtime` owns mediator behaviors, transactions, and post-commit execution. Module application projects own their use cases.
- Module infrastructure implements owner ports and provider adapters. Shared persistence coordinates module contexts; the primary database has one central migration history.
- `FoodDiary.ReadModel.Composition` provides approved cross-module reads. `FoodDiary.JobManager` runs recurring jobs, notification scheduling, and outbox processing.

The [architecture tests](Tooling/tests/FoodDiary.ArchitectureTests) enforce these boundaries. See [architecture](docs/ARCHITECTURE.md), [module ownership](docs/backend/BACKEND_MODULE_OWNERSHIP.md), and [ADRs](docs/adr/README.md) for the detailed contracts.

## Local Development

Run commands from the repository root unless a step explicitly changes directory.

### 1. Prepare the database and configuration

Start a local PostgreSQL 17 instance and create a `fooddiary` database. For a new database, with PostgreSQL command-line tools available:

```powershell
createdb --host=localhost --port=5432 --username=postgres fooddiary
```

In the PowerShell terminal that will run migrations and the API, replace `<local-password>` with your local PostgreSQL password:

```powershell
$env:DOTNET_ENVIRONMENT = 'Development'
$env:ConnectionStrings__DefaultConnection = 'Host=localhost;Port=5432;Database=fooddiary;Username=postgres;Password=<local-password>'
$jwtKeyBytes = [byte[]]::new(48)
[System.Security.Cryptography.RandomNumberGenerator]::Fill($jwtKeyBytes)
$env:Jwt__SecretKey = [Convert]::ToBase64String($jwtKeyBytes)
```

These overrides apply to the current terminal. For persistent configuration, use .NET user-secrets or local environment configuration. The API and Initializer have separate user-secrets stores; configuring one does not configure the other. See [API configuration](FoodDiary.Web.Api/CONFIGURATION.md) and [Initializer configuration](FoodDiary.Initializer/README.md#configuration).

Keep real credentials outside tracked files. JWT keys must be at least 32 characters long, have sufficient character diversity, and pass startup validation. The example generates a new key; retain your local key between runs to keep existing sign-in sessions valid.

### 2. Build, migrate, and start the API

In the same terminal:

```powershell
dotnet restore FoodDiary.slnx
dotnet build FoodDiary.slnx --no-restore
dotnet run --project FoodDiary.Initializer -- update --connection-string $env:ConnectionStrings__DefaultConnection
dotnet run --project FoodDiary.Web.Api --launch-profile http
```

The explicit connection string sends the Initializer to the same database as the API. Existing databases from before the migration baseline need the procedure in the [migration runbook](docs/backend/BACKEND_MIGRATION_SAFETY.md); the normal `update` command initializes new databases.

The API listens at [http://localhost:5300](http://localhost:5300). Its operational endpoints are [/health/live](http://localhost:5300/health/live) and [/health/ready](http://localhost:5300/health/ready).

In Development, an unset `ConnectionStrings:Redis` selects an in-memory cache; other environments require Redis. Configure email delivery, AI, image storage, and Telegram providers for the features you want to exercise. Background workflows also require JobManager. Provider and host details are linked from the [documentation index](docs/README.md).

### 3. Build and start the frontend

Open a second terminal at the repository root:

```powershell
cd FoodDiary.Web.Client
npm ci
npm run build
npm start
```

Open [http://localhost:4200](http://localhost:4200). The development client calls the API at `http://localhost:5300`; confirm API-backed pages can load data before treating the environment as ready.

For the admin app, run `npm run start:admin` in another terminal inside `FoodDiary.Web.Client` and open [http://localhost:4300](http://localhost:4300). See the [frontend guide](FoodDiary.Web.Client/AGENTS.md) and [frontend architecture](docs/frontend/FRONTEND_ARCHITECTURE.md) for additional projects and commands.

## Build and Verification

Backend build and focused architecture checks, from the repository root:

```powershell
dotnet build FoodDiary.slnx
dotnet test Tooling/tests/FoodDiary.ArchitectureTests/FoodDiary.ArchitectureTests.csproj
```

Run the complete backend suite with Docker available for integration tests, Node.js and frontend dependencies for MCP tests, and PowerShell for repository checks:

```powershell
dotnet test FoodDiary.slnx --maxcpucount:1
```

The [testing strategy](docs/TESTING_STRATEGY.md) describes focused suites, backend CI groups, integration prerequisites, coverage, and API contract snapshots. Shared MSBuild settings prune non-target SkiaSharp native assets; cross-platform publishes must specify the destination RID, such as `linux-x64`.

Frontend verification, from `FoodDiary.Web.Client` after `npm ci`:

```powershell
npm run verify
```

This checks generated SDKs, import boundaries, linting, styles, application/library builds, unit tests, and localization. Browser smoke tests have separate commands and runtime prerequisites in the [E2E guide](FoodDiary.Web.Client/e2e/README.md).

Git hooks are part of delivery: `pre-commit` checks staged whitespace; `pre-push` runs affected Wiki/frontend checks, backend build/format checks, and unit/architecture tests. Full integration and slow suites run in CI. Keep hooks enabled and run focused integration checks when their covered behavior changes.

## Deployment

[CI Tests](.github/workflows/ci-tests.yml) validates backend, frontend, generated API SDKs, and repository contracts. The [deploy workflow](.github/workflows/deploy.yml) follows successful CI for the selected revision and publishes container images.

The deployment uses [docker-compose.yml](docker-compose.yml) under `/opt/fooddiary` and server configuration from `/etc/fooddiary/fooddiary.env`. It coordinates primary database writers, takes a verified database backup, applies primary and mail-service migrations, and updates API, JobManager, Telegram, mail services, Redis, and RabbitMQ.

The `client` image supplies user/admin static assets and Storybook. Public recipe rendering also uses the `client-ssr` service. API, SSR, and edge readiness checks are part of the deployment workflow.

Use the [backend runbooks](docs/backend/BACKEND_RUNBOOKS.md), [migration safety guide](docs/backend/BACKEND_MIGRATION_SAFETY.md), [public recipe guide](docs/frontend/PUBLIC_RECIPES.md), and [observability configuration](infra/observability/README.md) for operational procedures.

## Further Reading

- [Documentation index](docs/README.md) — backend governance, frontend architecture, providers, operations, and active plans.
- [Testing strategy](docs/TESTING_STRATEGY.md) and [API contract governance](docs/backend/BACKEND_API_CONTRACT_GOVERNANCE.md).
- [Generated frontend API SDKs](docs/frontend/API_SDK.md).
- [Backend definition of done](docs/backend/BACKEND_DEFINITION_OF_DONE.md) and [security hardening](docs/backend/BACKEND_SECURITY_HARDENING.md).
- [AI change workflow](docs/ai/AI_WORKFLOW.md) and [agent knowledge index](.llm-wiki/index.md). The Wiki is derived navigation; current code, tests, ADRs, docs, and scoped guides remain authoritative.
