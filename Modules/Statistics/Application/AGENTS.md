# Statistics Application Guidelines

## Scope

Rules for `Modules/Statistics/Application/`.

## Boundary

- Own statistics queries, summary composition, response models, and date-range normalization.
- Consume dashboard through a direct Dashboard.Contracts reference and body-metric/user capabilities through their existing contracts; never reference foreign application implementations.
- Keep persistence implementations and HTTP transport outside this project.
- Do not reference the legacy `FoodDiary.Application` project.
- Preserve the legacy `FoodDiary.Application.Statistics` assembly name and CLR namespaces.

## Commands

- Build: `dotnet build Modules/Statistics/Application/FoodDiary.Modules.Statistics.Application.csproj`
- Tests: `dotnet test Modules/Statistics/tests/FoodDiary.Modules.Statistics.Application.Tests/FoodDiary.Modules.Statistics.Application.Tests.csproj`
