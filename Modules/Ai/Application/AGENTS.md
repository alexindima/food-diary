# AI Application Module Guidelines

## Scope

Rules for `Modules/Ai/Application/`.

## Boundary

- Own AI use cases, quota policy, prompt administration, usage projections, and application telemetry.
- Consume image and user capabilities only through their narrow owner contracts.
- Keep provider SDKs, HTTP clients, and persistence implementations outside this project.
- Do not reference the legacy `FoodDiary.Application` project.

## Commands

- Build: `dotnet build Modules/Ai/Application/FoodDiary.Modules.Ai.Application.csproj`
- Tests: `dotnet test Modules/Ai/tests/FoodDiary.Modules.Ai.Application.Tests/FoodDiary.Modules.Ai.Application.Tests.csproj`
