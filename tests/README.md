# Tests Guide

## Structure
- `FoodDiary.ArchitectureTests`:
  - Verifies layering and feature-structure constraints.
  - Should not depend on runtime infrastructure or external services.
- `FoodDiary.Application.Tests`:
  - Unit tests for domain/application logic (invariants, validators, handlers, services).
  - Keep tests fast and deterministic.
- `FoodDiary.Web.Api.IntegrationTests`:
  - API-level integration tests via `WebApplicationFactory`.
  - Uses in-memory database setup in test host.

## Where To Add New Tests
- New domain invariant/event behavior: `FoodDiary.Application.Tests/Domain/*`
- New application use-case/handler logic: `FoodDiary.Application.Tests/<Feature>/*`
- New API endpoint flow/auth contract: `FoodDiary.Web.Api.IntegrationTests/*`
- New architecture rule: `FoodDiary.ArchitectureTests/*`
- Backend HTTP contract changes should also review/update snapshots and PR notes per `BACKEND_API_CONTRACT_GOVERNANCE.md`

## Local Commands
- Full build and tests:
  - `dotnet restore FoodDiary.slnx`
  - `dotnet build FoodDiary.slnx --configuration Release --no-restore`
  - `dotnet test tests/FoodDiary.ArchitectureTests/FoodDiary.ArchitectureTests.csproj --configuration Release --no-restore`
  - `dotnet test tests/FoodDiary.Application.Tests/FoodDiary.Application.Tests.csproj --configuration Release --no-restore`
  - `dotnet test tests/FoodDiary.Web.Api.IntegrationTests/FoodDiary.Web.Api.IntegrationTests.csproj --configuration Release --no-restore`

## CI
- Workflow: `.github/workflows/ci-tests.yml`
- Order: restore -> build -> architecture tests -> application tests -> integration tests.

## Conventions
- Prefer AAA pattern and explicit test names (`Given_When_Then` or equivalent descriptive style).
- One behavior per test; avoid broad scenario coupling.
- Do not use real external services in tests.
- Keep assertions focused on business behavior and contracts.
