# Images Application Module Guidelines

## Scope

Rules for `FoodDiary.Application.Images/`.

## Boundary

- Own image-asset commands, resolution, access, and cleanup policies.
- Expose cross-module capabilities through `FoodDiary.Application.Abstractions/Images`.
- Keep storage providers and persistence implementations outside this project.
- Do not reference the legacy `FoodDiary.Application` project.

## Commands

- Build: `dotnet build FoodDiary.Application.Images/FoodDiary.Application.Images.csproj`
- Tests: `dotnet test tests/FoodDiary.Application.Tests/FoodDiary.Application.Tests.csproj --filter FullyQualifiedName~Images`
