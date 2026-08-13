# AI Application Module Guidelines

## Scope

Rules for `FoodDiary.Application.Ai/`.

## Boundary

- Own AI use cases, quota policy, prompt administration, usage projections, and application telemetry.
- Consume image and user capabilities only through `FoodDiary.Application.Abstractions` contracts.
- Keep provider SDKs, HTTP clients, and persistence implementations outside this project.
- Do not reference the legacy `FoodDiary.Application` project.

## Commands

- Build: `dotnet build FoodDiary.Application.Ai/FoodDiary.Application.Ai.csproj`
- Tests: `dotnet test tests/FoodDiary.Application.Tests/FoodDiary.Application.Tests.csproj --filter FullyQualifiedName~Ai`
