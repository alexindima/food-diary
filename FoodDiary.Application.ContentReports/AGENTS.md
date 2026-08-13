# Content Reports Application Module Guidelines

## Scope

Rules for `FoodDiary.Application.ContentReports/`.

## Boundary

- Own content-report commands, administration services, models, and validators.
- Depend only on application abstractions, domain types, and the shared mediator.
- Keep persistence implementations in `FoodDiary.Infrastructure` and HTTP transport in presentation projects.
- Expose projection reads through `IContentReportReadModelRepository` and mutations through `IContentReportWriteRepository`; do not recreate a composite repository.

## Commands

- Build: `dotnet build FoodDiary.Application.ContentReports/FoodDiary.Application.ContentReports.csproj`
- Tests: `dotnet test tests/FoodDiary.Application.Tests/FoodDiary.Application.Tests.csproj --filter FullyQualifiedName~ContentReports`
- Guardrails: `dotnet test tests/FoodDiary.ArchitectureTests/FoodDiary.ArchitectureTests.csproj`
