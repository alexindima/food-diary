# Dietologist Application Module Guidelines

## Scope

Rules for `FoodDiary.Application.Dietologist/`.

## Boundaries

- This project owns Dietologist application use cases, policies, mappings, models, and services.
- Depend only on `FoodDiary.Application.Abstractions`, `FoodDiary.Domain`, and `FoodDiary.Mediator`.
- Interact with Users, Dashboard, Notifications, Audit, and persistence only through contracts in `FoodDiary.Application.Abstractions`.
- Do not reference `FoodDiary.Application`, infrastructure, presentation, hosts, or provider SDKs.
- Register module-owned handlers, validators, and services through `AddDietologistModule()`.

## Commands

- Build: `dotnet build FoodDiary.Application.Dietologist/FoodDiary.Application.Dietologist.csproj`
- Tests: `dotnet test tests/FoodDiary.Application.Tests/FoodDiary.Application.Tests.csproj --filter FullyQualifiedName~Dietologist`
- Guardrails: `dotnet test tests/FoodDiary.ArchitectureTests/FoodDiary.ArchitectureTests.csproj`
