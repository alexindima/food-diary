# Billing Application Module Guidelines

## Scope

Rules for `FoodDiary.Application.Billing/`.

## Responsibilities

- Own billing commands, queries, models, application services, and webhook orchestration.
- Register handlers, validators, and services through `AddBillingModule`.
- Depend on application-facing ports rather than infrastructure or provider implementations.

## Rules

- This is an extracted leaf module. The target dependency set is Application Abstractions, Domain, and the shared mediator.
- Do not reference presentation, infrastructure, integrations, or executable hosts.
- Provider idempotency and webhook ordering must remain explicit and covered by concurrency tests.
- Billing commands that mutate state implement the abstraction-level transactional command marker.

## Commands

- Build: `dotnet build FoodDiary.Application.Billing/FoodDiary.Application.Billing.csproj`
- Tests: `dotnet test tests/FoodDiary.Application.Tests/FoodDiary.Application.Tests.csproj`
- Guardrails: `dotnet test tests/FoodDiary.ArchitectureTests/FoodDiary.ArchitectureTests.csproj`
