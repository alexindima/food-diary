# Tests

7 test projects, all using **xUnit** exclusively. **No mocking framework** — all test doubles are hand-written.

## Test Projects

| Project | Category | Key Tech |
|---------|----------|----------|
| `FoodDiary.Application.Tests` | Unit | Domain invariants, handler tests, validator tests |
| `FoodDiary.Presentation.Api.Tests` | Unit | Controller tests, error mapping, DTO mapping, security contract reflection |
| `FoodDiary.Telegram.Bot.Tests` | Unit | Bot utility tests |
| `FoodDiary.JobManager.Tests` | Unit | Options validation |
| `FoodDiary.MailRelay.Tests` | Integration | WebApplicationFactory + Testcontainers PostgreSQL/RabbitMQ |
| `FoodDiary.Infrastructure.Tests` | Unit + Integration | JWT round-trip, Testcontainers PostgreSQL, migration safety |
| `FoodDiary.Web.Api.IntegrationTests` | Integration | WebApplicationFactory, API contract snapshots |
| `FoodDiary.ArchitectureTests` | Architecture guardrails | Layering, naming, forbidden patterns |

## Key Conventions

### No Mocking Framework
All test doubles are **nested private sealed classes** inside test classes:
- `Fake*` — records interactions (e.g., `FakeJwtTokenGenerator`)
- `Stub*` — returns canned responses (e.g., `StubDateTimeProvider`)
- `InMemory*` — stateful in-memory implementations (e.g., `InMemoryUserRepository`)
- Unused interface methods throw `NotSupportedException`

### Self-Contained Tests
Each test class defines its own fakes inline. No shared test utility library.

### xUnit Only
- `[Fact]` is preferred over `[Theory]`
- Only xUnit built-in assertions (`Assert.Equal`, `Assert.Throws<T>`, `Assert.Contains`, etc.)
- No FluentAssertions or Shouldly

### Integration Test Infrastructure
- **Testcontainers**: `PostgresDatabaseFixture` starts `postgres:17-alpine`, creates isolated databases, applies migrations. Shared via `[Collection("postgres-database")]`
- **`RequiresDockerFact`**: custom attribute that skips tests when Docker is unavailable
- **WebApplicationFactory**: `ApiWebApplicationFactory` replaces DbContext with InMemory, stubs S3. `TestAuthApiWebApplicationFactory` adds header-based auth bypass (`X-Test-Auth`, `X-Test-UserId`, `X-Test-Role`)
- **Snapshot Testing**: JSON snapshots in `Web.Api.IntegrationTests/Snapshots/` for API contract verification
- **Contract Snapshot Rule**: when public API contract changes intentionally, update and commit the affected snapshot JSON files; CI expects them to exist in the repo checkout

### Options in Tests
Use `Options.Create(new T { ... })` to create `IOptions<T>` directly.

### Protected Method Testing
`TestController` subclass pattern exposes protected methods as public wrappers (e.g., for `BaseApiController`).

## Architecture Tests (`FoodDiary.ArchitectureTests/`)

Enforce structural rules by scanning .csproj files and source code:
- **LayeringTests** — Domain references nothing, Application only references Domain, Infrastructure doesn't reference Web.Api, Presentation.Api doesn't import Domain namespaces
- **FeatureStructureTests** — feature folders contain Commands/Queries/Common, namespaces match folder paths
- **DomainModelGuardrailTests** — aggregate mutators max 8 parameters
- **ApplicationGuardrailTests** — ban `Enum.Parse(`, `DateTime.UtcNow`, `CancellationToken.None`, `IOptions<`, `IConfiguration`, infrastructure references; async interfaces must accept CancellationToken; repository interfaces scoped to their feature
- **PresentationConventionsTests** — controllers use Handle* helpers, no ad-hoc status returns, no claim parsing, no Contracts namespace

## Running Tests

```bash
dotnet test FoodDiary.slnx                    # All tests
dotnet test tests/FoodDiary.ArchitectureTests  # Architecture guardrails only
```

Docker required for Infrastructure integration tests (gracefully skipped if unavailable).
