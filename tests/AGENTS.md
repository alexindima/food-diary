# Test Guidelines

## Scope
Rules for `tests/`.

## Role
- Keep tests aligned with the production project they protect.
- Use tests as living architecture documentation when updating project guides.

## Test Types
- `FoodDiary.ArchitectureTests`: dependency, structure, naming, async, and boundary guardrails.
- `FoodDiary.Web.Api.IntegrationTests`: HTTP contract, OpenAPI, and end-to-end API host behavior.
- `FoodDiary.Web.Api.Tests`: Web.Api host options, middleware, health check, and service unit behavior.
- `FoodDiary.Presentation.Api.Tests`: controller/presentation mapping and error response behavior.
- `FoodDiary.Application.Tests`: use case and application service behavior.
- `FoodDiary.Domain.Tests`: core domain entity, value object, domain event, and invariant behavior.
- `FoodDiary.Domain.Primitives.Tests`: shared domain primitive behavior.
- `FoodDiary.Infrastructure.Tests`: infrastructure unit behavior that does not require external services.
- `FoodDiary.Infrastructure.IntegrationTests`: PostgreSQL/Testcontainers infrastructure behavior.
- `FoodDiary.Testing`: shared test-only helpers reused by multiple test projects, such as Docker availability attributes.
- `FoodDiary.Results.Tests`: shared result and error primitive behavior.
- Mail relay/inbox tests: split by domain, application, client, infrastructure, initializer, presentation, and integration behavior.

## Rules
- Prefer focused tests near the layer being changed.
- Use NSubstitute for simple interface substitutes in unit tests when it avoids noisy hand-written `Fake`/`Stub`/`Recording` types.
- Keep hand-written `InMemory`/`Recording` helpers when they make stateful behavior, call history, or side effects clearer than a mock setup.
- Prefer shared assertion helpers for common result shapes. In `FoodDiary.Application.Tests`, use `ResultAssert.Success(...)` and `ResultAssert.Failure(...)` instead of bare `Assert.True(result.IsSuccess)` / `Assert.True(result.IsFailure)` so failures include useful error context.
- Use `Assert.Multiple(...)` for groups of independent assertions over an already-created result, especially DTO, HTTP response, read-model, mapping, and domain-event field coverage. Keep assert-and-extract steps outside `Assert.Multiple(...)`: use plain `Assert.Single`, `Assert.IsType`, `Assert.NotNull`, `ResultAssert.Success(...)`, and similar guards first, then wrap the independent field checks.
- Avoid raw sleeps in async tests. Prefer a task-completion signal with a bounded wait helper and a failure message; use polling only when checking an external resource such as a TCP port or broker message, and keep the timeout explicit.
- When feature-test files grow large, split new coverage by command/query/service instead of adding unrelated scenarios to an already-large file.
- Do not replace persistence, HTTP contract, or other integration coverage with mocks; keep Testcontainers/Postgres and WebApplicationFactory tests for behavior that depends on real infrastructure.
- For HTTP contract changes, update snapshots under `tests/FoodDiary.Web.Api.IntegrationTests/Snapshots/`.
- Do not weaken architecture tests to make a feature pass; update the architecture intentionally and document why.
- Keep test helpers local to the test project unless reuse is clear; shared helpers belong in `FoodDiary.Testing`.
- Keep test project references aligned with the architecture-test dependency matrix.
- Mark every test type and test-only helper type with `[ExcludeFromCodeCoverage]` so test implementation details stay out of dotCover reports.

## Commands
- Architecture tests: `dotnet test tests/FoodDiary.ArchitectureTests/FoodDiary.ArchitectureTests.csproj`
- Shared domain primitive tests: `dotnet test tests/FoodDiary.Domain.Primitives.Tests/FoodDiary.Domain.Primitives.Tests.csproj`
- Core domain tests: `dotnet test tests/FoodDiary.Domain.Tests/FoodDiary.Domain.Tests.csproj`
- Shared result tests: `dotnet test tests/FoodDiary.Results.Tests/FoodDiary.Results.Tests.csproj`
- Web API unit tests: `dotnet test tests/FoodDiary.Web.Api.Tests/FoodDiary.Web.Api.Tests.csproj`
- API integration tests: `dotnet test tests/FoodDiary.Web.Api.IntegrationTests/FoodDiary.Web.Api.IntegrationTests.csproj`
- Infrastructure unit tests: `dotnet test tests/FoodDiary.Infrastructure.Tests/FoodDiary.Infrastructure.Tests.csproj`
- Infrastructure integration tests: `dotnet test tests/FoodDiary.Infrastructure.IntegrationTests/FoodDiary.Infrastructure.IntegrationTests.csproj`
- Full backend test/build baseline: `dotnet build FoodDiary.slnx`
