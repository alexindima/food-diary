# Recipes Application Module Guidelines

## Scope

Rules for `FoodDiary.Application.Recipes/`.

## Boundary

- Own Recipe commands, queries, mappings, recent-recipe orchestration, nutrition calculation, and Recipe mutation capabilities.
- Expose cross-module Recipe reads through stable models and contracts from `FoodDiary.Application.Abstractions`.
- Keep Recipe aggregate loading and mutation behind Recipes-owned repository or capability contracts.
- Other modules must not receive or mutate Recipe aggregates.
- Register through `AddRecipesModule`; executable hosts remain composition roots.

## Verification

- Build: `dotnet build FoodDiary.Application.Recipes/FoodDiary.Application.Recipes.csproj`
- Focused tests: `dotnet test tests/FoodDiary.Application.Tests/FoodDiary.Application.Tests.csproj --filter FullyQualifiedName~Recipes`
- Architecture: `dotnet test tests/FoodDiary.ArchitectureTests/FoodDiary.ArchitectureTests.csproj`
