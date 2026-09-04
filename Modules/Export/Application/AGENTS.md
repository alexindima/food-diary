# Export Application Guidelines

## Scope

Rules for `Modules/Export/Application/`.

## Boundary

- Own diary and cycle export queries, validation, file-result models, and CSV generation.
- Preserve the legacy `FoodDiary.Application.Export` assembly name and CLR namespaces.
- Consume Cycles and Meals only through their application-level read capabilities.
- Reference Cycles Domain directly only for cycle enums already exposed by those read models; do not acquire Cycles aggregates or repositories.
- Reference Meals Domain directly for MealType already exposed by diary read models; preserve CSV formatting and continue to use existing read capabilities.
- Reference Identity Application/Abstractions directly for the existing AuthenticationInputLimits constants used by sensitive-cycle validation; const inlining must not hide the owning project dependency.
- Keep PDF rendering implementations in the sibling Export Infrastructure project and HTTP controllers in Presentation; neither belongs in Application.
- Do not reference the core `FoodDiary.Application` project.

## Commands

- Build: `dotnet build Modules/Export/Application/FoodDiary.Modules.Export.Application.csproj`
- Tests: `dotnet test Modules/Export/tests/FoodDiary.Modules.Export.Application.Tests/FoodDiary.Modules.Export.Application.Tests.csproj`
- Guardrails: `dotnet test tests/FoodDiary.ArchitectureTests/FoodDiary.ArchitectureTests.csproj`
