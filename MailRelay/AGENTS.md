# Mail Relay Guidelines

## Scope
Rules for `MailRelay/`.

## Role
- Treat MailRelay as a separate service boundary with its own domain, database, host, client package, initializer, infrastructure, presentation, and tests.
- Prefer the layer-specific `AGENTS.md` file in each child project for concrete rules.
- Keep MailRelay changes inside this folder unless integration with the primary FoodDiary app explicitly requires a client-package call from `FoodDiary.Integrations`.

## Project Guides
- Application layer: `FoodDiary.MailRelay.Application/AGENTS.md`
- Client package: `FoodDiary.MailRelay.Client/AGENTS.md`
- Domain layer: `FoodDiary.MailRelay.Domain/AGENTS.md`
- Infrastructure layer: `FoodDiary.MailRelay.Infrastructure/AGENTS.md`
- Initializer: `FoodDiary.MailRelay.Initializer/AGENTS.md`
- Presentation layer: `FoodDiary.MailRelay.Presentation/AGENTS.md`
- Web API host: `FoodDiary.MailRelay.WebApi/AGENTS.md`
- Tests: `tests/AGENTS.md`

## Cross-Service Rules
- Keep executable hosts as composition roots.
- Keep PostgreSQL queue state as the source of truth even when RabbitMQ is active.
- Keep HTTP payload mapping in presentation, use cases in application, delivery/storage/provider implementations in infrastructure, and relay business rules in domain.
- Do not let primary FoodDiary core projects reference MailRelay internals directly; use `FoodDiary.MailRelay.Client` through approved integration boundaries.

## Commands
- Build service projects: `dotnet build FoodDiary.slnx`
- Service integration tests: `dotnet test MailRelay/tests/FoodDiary.MailRelay.IntegrationTests/FoodDiary.MailRelay.IntegrationTests.csproj`
- Architecture guardrails: `dotnet test tests/FoodDiary.ArchitectureTests/FoodDiary.ArchitectureTests.csproj`
