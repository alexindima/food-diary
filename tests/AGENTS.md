# Test Guidelines

## Scope
Rules for `tests/`.

## Role
- Keep tests aligned with the production project they protect.
- Use tests as living architecture documentation when updating project guides.

## Test Types
- `FoodDiary.ArchitectureTests`: dependency, structure, naming, async, and boundary guardrails.
- `FoodDiary.Web.Api.IntegrationTests`: HTTP contract, OpenAPI, and end-to-end API host behavior.
- `FoodDiary.Presentation.Api.Tests`: controller/presentation mapping and error response behavior.
- `FoodDiary.Application.Tests`: use case and application service behavior.
- `FoodDiary.Infrastructure.Tests`: persistence/infrastructure behavior.
- Mail relay/inbox tests: service-specific use case, infrastructure, presentation, and client behavior.

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
- Keep test helpers local to the test project unless reuse is clear.
- Mark every test type and test-only helper type with `[ExcludeFromCodeCoverage]` so test implementation details stay out of dotCover reports.

## Commands
- Architecture tests: `dotnet test tests/FoodDiary.ArchitectureTests/FoodDiary.ArchitectureTests.csproj`
- API integration tests: `dotnet test tests/FoodDiary.Web.Api.IntegrationTests/FoodDiary.Web.Api.IntegrationTests.csproj`
- Full backend test/build baseline: `dotnet build FoodDiary.slnx`
