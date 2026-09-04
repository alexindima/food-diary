# USDA Logical Module Guidelines

## Boundary

- Own USDA application flows, provider/persistence ports, cross-module models, EF mappings, repository adapter, and focused application tests.
- Keep USDA HTTP requests, provider DTO mapping, timeout, cancellation, in-memory detail cache, and API-key options in `FoodDiary.Integrations`.
- Keep USDA reference-data entities in the module Domain while preserving their CLR and EF identity. Central Domain references the module Domain one-way for `Product.UsdaFood`; never add the reverse dependency.
- Keep `FoodDiaryDbContext`, migrations, and snapshot central. Register mappings explicitly with `ApplyUsdaPersistenceModel`.
- Preserve the `FoodDiary.Application.Usda` assembly and all legacy CLR namespaces.

## Verification

- `dotnet test Modules/Usda/tests/FoodDiary.Modules.Usda.Application.Tests/FoodDiary.Modules.Usda.Application.Tests.csproj`
- `dotnet test tests/FoodDiary.Infrastructure.Tests/FoodDiary.Infrastructure.Tests.csproj --filter FullyQualifiedName~UsdaFoodSearchServiceTests`
- `dotnet test tests/FoodDiary.ArchitectureTests/FoodDiary.ArchitectureTests.csproj`

## Error ownership

Feature error factories belong to their existing owner contracts; call them directly.
The corresponding central Errors facades are retired. Preserve exact codes, messages,
kinds and parameter formatting. Reference the owner explicitly; this grants no foreign
repository or aggregate capability. See docs/ai/feature-error-retirement.md.
