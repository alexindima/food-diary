# Mail Relay Test Guidelines

## Scope
Rules for `MailRelay/tests/`.

## Role
- Keep MailRelay tests focused on service behavior across domain, application, infrastructure, presentation, client, and host integration.
- Use integration tests for PostgreSQL queue/outbox/inbox behavior, RabbitMQ dispatch behavior, HTTP contracts, and provider webhook mappings.
- Use unit tests for domain policies, application validation, options validation, transport selection, and delivery message construction.

## Rules
- Mark every test type and test-only helper type with `[ExcludeFromCodeCoverage]`.
- Keep Docker-dependent tests behind the local Docker availability helper.
- Prefer asserting persisted queue state over only asserting RabbitMQ messages, because PostgreSQL queue state is the source of truth.
- Add regression tests for retry, idempotency, suppression, and provider webhook behavior whenever those paths change.

## Commands
- Application tests: `dotnet test MailRelay/tests/FoodDiary.MailRelay.Application.Tests/FoodDiary.MailRelay.Application.Tests.csproj`
- Infrastructure tests: `dotnet test MailRelay/tests/FoodDiary.MailRelay.Infrastructure.Tests/FoodDiary.MailRelay.Infrastructure.Tests.csproj`
- Integration tests: `dotnet test MailRelay/tests/FoodDiary.MailRelay.IntegrationTests/FoodDiary.MailRelay.IntegrationTests.csproj`
- Architecture tests: `dotnet test tests/FoodDiary.ArchitectureTests/FoodDiary.ArchitectureTests.csproj`
