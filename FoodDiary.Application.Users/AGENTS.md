# Users Application Module Guidelines

## Scope

Rules for `FoodDiary.Application.Users/`.

## Boundaries

- Own Users commands, queries, mappings, policies, and application services.
- Depend only on `FoodDiary.Application.Abstractions`, `FoodDiary.Domain`, and `FoodDiary.Mediator`.
- Other application modules consume Users capabilities through abstractions; they must not reference this implementation project.
- Register module handlers and services through `AddUsersModule()` from executable composition roots.

## Commands

- Build: `dotnet build FoodDiary.Application.Users/FoodDiary.Application.Users.csproj`
- Tests: `dotnet test tests/FoodDiary.Application.Tests/FoodDiary.Application.Tests.csproj`
- Guardrails: `dotnet test tests/FoodDiary.ArchitectureTests/FoodDiary.ArchitectureTests.csproj`
