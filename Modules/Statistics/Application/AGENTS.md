# Statistics Application Guidelines

## Scope

Rules for `Modules/Statistics/Application/`.

## Boundary

- Own statistics queries, summary composition, response models, and date-range normalization.
- Consume dashboard through a direct Dashboard.Contracts reference and body-metric/user capabilities through their existing contracts; never reference foreign application implementations.
- Keep persistence implementations and HTTP transport outside this project.
- Do not reference the legacy `FoodDiary.Application` project.
- Use canonical `FoodDiary.Modules.Statistics.<Project>` assembly identities and folder namespaces.

## Commands

- Build: `dotnet build Modules/Statistics/Application/FoodDiary.Modules.Statistics.Application.csproj`
- Tests: `dotnet test Modules/Statistics/tests/FoodDiary.Modules.Statistics.Application.Tests/FoodDiary.Modules.Statistics.Application.Tests.csproj`

Current module convention: all projects use `FoodDiary.Modules.Statistics.<Project>` assembly identities and namespaces matching their folders, including tests. Preserve historical migration metadata and database/HTTP contracts during namespace moves.
