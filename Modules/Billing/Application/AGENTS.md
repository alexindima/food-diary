# Billing Application Module Guidelines

## Scope

Rules for `Modules/Billing/Application/`.

## Responsibilities

- Own billing commands, queries, models, application services, and webhook orchestration.
- Register handlers, validators, and services through `AddBillingApplication`; Infrastructure exposes `AddBillingModule`.
- Depend on application-facing ports rather than infrastructure or provider implementations.

## Rules

- This is an extracted leaf module. The target dependency set is Application Abstractions, Domain, and the shared mediator.
- Do not reference presentation, infrastructure, integrations, or executable hosts.
- Provider idempotency and webhook ordering must remain explicit and covered by concurrency tests.
- Billing commands that mutate state implement the abstraction-level transactional command marker.

## Commands

- Build: `dotnet build Modules/Billing/Application/FoodDiary.Application.Billing.csproj`
- Tests: `dotnet test Modules/Billing/tests/FoodDiary.Modules.Billing.Application.Tests/FoodDiary.Modules.Billing.Application.Tests.csproj`
- Guardrails: `dotnet test tests/FoodDiary.ArchitectureTests/FoodDiary.ArchitectureTests.csproj`
