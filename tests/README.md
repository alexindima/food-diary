# Tests Guide

## Structure
- `FoodDiary.ArchitectureTests`:
  - Verifies layering and feature-structure constraints.
  - Should not depend on runtime infrastructure or external services.
- `FoodDiary.Application.Tests`:
  - Unit tests for application logic (validators, handlers, services).
  - Keep tests fast and deterministic.
- `FoodDiary.Domain.Tests`:
  - Unit tests for core domain invariants, value objects, entities, and domain events.
  - Keep tests fast and deterministic.
- `FoodDiary.Infrastructure.Tests`:
  - Unit tests for infrastructure services, EF helpers, dependency injection, and provider adapters.
- `FoodDiary.Infrastructure.IntegrationTests`:
  - PostgreSQL/Testcontainers tests for persistence and migration behavior.
- `FoodDiary.Web.Api.Tests`:
  - Unit tests for host options, middleware, health checks, and Web.Api services.
- `FoodDiary.Web.Api.IntegrationTests`:
  - API-level integration tests via `WebApplicationFactory`.
  - Uses in-memory database setup in test host.
- `MailRelay/tests/FoodDiary.MailRelay.*.Tests`:
  - Unit tests split by domain, application, client, infrastructure, initializer, and presentation layers.
- `MailRelay/tests/FoodDiary.MailRelay.IntegrationTests`:
  - Relay-level integration tests via `WebApplicationFactory`.
  - Uses PostgreSQL + RabbitMQ Testcontainers and a fake delivery transport.
- `MailInbox/tests/FoodDiary.MailInbox.*.Tests`:
  - Unit tests split by domain, application, client, infrastructure, initializer, and presentation layers.
- `MailInbox/tests/FoodDiary.MailInbox.IntegrationTests`:
  - PostgreSQL-backed mail inbox persistence tests.

## Where To Add New Tests
- New core domain invariant/event behavior: `FoodDiary.Domain.Tests/Domain/*`
- New application use-case/handler logic: `FoodDiary.Application.Tests/<Feature>/*`
- New infrastructure behavior without real PostgreSQL: `FoodDiary.Infrastructure.Tests/*`
- New infrastructure behavior requiring PostgreSQL/Testcontainers: `FoodDiary.Infrastructure.IntegrationTests/Integration/*`
- New Web.Api host/middleware/service unit behavior: `FoodDiary.Web.Api.Tests/*`
- New API endpoint flow/auth contract: `FoodDiary.Web.Api.IntegrationTests/*`
- New mail relay broker/queue flow: `MailRelay/tests/FoodDiary.MailRelay.IntegrationTests/*`
- New architecture rule: `FoodDiary.ArchitectureTests/*`
- Backend HTTP contract changes should also review/update snapshots and PR notes per `../docs/backend/BACKEND_API_CONTRACT_GOVERNANCE.md`
- Swagger/OpenAPI contract changes must update the checked-in snapshot files in `FoodDiary.Web.Api.IntegrationTests/Snapshots/`
- Presentation-layer filter/transport behavior such as idempotency, controller filters, and HTTP caching semantics belongs in `FoodDiary.Presentation.Api.Tests/*`

## Local Commands
- Full build and tests:
  - `dotnet restore FoodDiary.slnx`
  - `dotnet build FoodDiary.slnx --configuration Release --no-restore`
  - `dotnet test tests/FoodDiary.ArchitectureTests/FoodDiary.ArchitectureTests.csproj --configuration Release --no-restore`
  - `dotnet test tests/FoodDiary.Domain.Tests/FoodDiary.Domain.Tests.csproj --configuration Release --no-restore`
  - `dotnet test tests/FoodDiary.Application.Tests/FoodDiary.Application.Tests.csproj --configuration Release --no-restore`
  - `dotnet test tests/FoodDiary.Infrastructure.Tests/FoodDiary.Infrastructure.Tests.csproj --configuration Release --no-restore`
  - `dotnet test tests/FoodDiary.Web.Api.Tests/FoodDiary.Web.Api.Tests.csproj --configuration Release --no-restore`
  - `dotnet test tests/FoodDiary.Web.Api.IntegrationTests/FoodDiary.Web.Api.IntegrationTests.csproj --configuration Release --no-restore`

## CI
- Workflow: `.github/workflows/ci-tests.yml`
- Order: restore -> build -> architecture tests -> application tests -> integration tests.

## Conventions
- Prefer AAA pattern and explicit test names (`Given_When_Then` or equivalent descriptive style).
- One behavior per test; avoid broad scenario coupling.
- Do not use real external services in tests.
- Keep assertions focused on business behavior and contracts.
