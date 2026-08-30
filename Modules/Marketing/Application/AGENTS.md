# Marketing Application Module Guidelines

## Scope

Rules for `Modules/Marketing/Application/`.

## Responsibilities

- Own marketing attribution commands, queries, models, and semantic services.
- Register module handlers and services through `AddMarketingApplication`; Infrastructure exposes the complete `AddMarketingModule` facade.
- Implement consumer-owned ports such as `IBillingMarketingConversionRecorder` without exposing persistence contracts.

## Rules

- Keep this project as an isolated leaf application module: it may reference Application Abstractions, shared mediator/results transitively, but it must not reference the core Application assembly.
- Do not move Marketing handlers or models back into `FoodDiary.Application/Marketing`.
- Do not acquire repositories owned by other modules.
- Add new runtime consumers explicitly to the project dependency matrix and call `AddMarketingModule` in executable composition roots.

## Commands

- Build: `dotnet build Modules/Marketing/Application/FoodDiary.Application.Marketing.csproj`
- Application tests: `dotnet test Modules/Marketing/tests/FoodDiary.Modules.Marketing.Application.Tests/FoodDiary.Modules.Marketing.Application.Tests.csproj`
- Architecture tests: `dotnet test tests/FoodDiary.ArchitectureTests/FoodDiary.ArchitectureTests.csproj`
