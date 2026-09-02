# Users Application Module Guidelines

## Scope

Rules for `Modules/Users/Application/`.

## Boundaries

- Own Users commands, queries, mappings, policies, and application services.
- Depend on Users Domain/Domain.Contracts, Images Domain, shared application abstractions and Mediator, as declared in the project reference matrix.
- Reference Users Domain.Contracts directly for the existing ActivityLevel profile enum and UserId; preserve parsing, defaults and profile/TDEE behavior.
- Other application modules consume Users capabilities through abstractions; they must not reference this implementation project.
- Register handlers and services through `AddUsersApplication`; executable composition roots use Infrastructure's `AddUsersModule` facade.

## Commands

- Build: `dotnet build Modules/Users/Application/FoodDiary.Modules.Users.Application.csproj`
- Tests: `dotnet test Modules/Users/tests/FoodDiary.Modules.Users.Application.Tests/FoodDiary.Modules.Users.Application.Tests.csproj`
- Guardrails: `dotnet test tests/FoodDiary.ArchitectureTests/FoodDiary.ArchitectureTests.csproj`
