# Marketing Application Module Guidelines

## Scope

Rules for `FoodDiary.Application.Marketing/`.

## Responsibilities

- Own marketing attribution commands, queries, models, and semantic services.
- Register module handlers and services through `AddMarketingModule`.
- Implement consumer-owned ports such as `IBillingMarketingConversionRecorder` without exposing persistence contracts.

## Rules

- Keep this project as an isolated leaf application module: it may reference Application Abstractions, shared mediator/results transitively, but it must not reference the core Application assembly.
- Do not move Marketing handlers or models back into `FoodDiary.Application/Marketing`.
- Do not acquire repositories owned by other modules.
- Add new runtime consumers explicitly to the project dependency matrix and call `AddMarketingModule` in executable composition roots.

## Commands

- Build: `dotnet build FoodDiary.Application.Marketing/FoodDiary.Application.Marketing.csproj`
- Application tests: `dotnet test tests/FoodDiary.Application.Tests/FoodDiary.Application.Tests.csproj`
- Architecture tests: `dotnet test tests/FoodDiary.ArchitectureTests/FoodDiary.ArchitectureTests.csproj`
