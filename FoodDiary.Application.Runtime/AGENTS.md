# Application Runtime Guidelines

## Scope

Rules for `FoodDiary.Application.Runtime/`.

## Role

- Own only cross-cutting application execution behavior and runtime service registration.
- Keep business use cases and feature service registration in the owning application module.

## Boundaries

- Depend only on `FoodDiary.Application.Abstractions` and `Shared/FoodDiary.Mediator`.
- Do not reference feature application modules, domain, infrastructure, presentation, resources, or executable hosts.
- Keep external provider SDKs, HTTP clients, EF Core, and host configuration outside this project.

## Composition

- Keep `DependencyInjection.cs` limited to mediator pipeline behaviors and cross-cutting runtime services.
- Feature modules register their own handlers and validators; executable hosts compose those registrations.
- Do not turn this project back into a feature-module aggregator.

## Commands

- Build: `dotnet build FoodDiary.Application.Runtime/FoodDiary.Application.Runtime.csproj`
- Tests: `dotnet test tests/FoodDiary.Application.Tests/FoodDiary.Application.Tests.csproj`
- Architecture guardrails: `dotnet test tests/FoodDiary.ArchitectureTests/FoodDiary.ArchitectureTests.csproj`
