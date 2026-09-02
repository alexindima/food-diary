# Export Logical Module Guidelines

## Scope

Rules for `Modules/Export/`.

## Boundary

- Own diary and cycle export queries, validation, file-result models, CSV generation, and export-specific adapter contracts.
- Preserve the legacy `FoodDiary.Application.Export` assembly name and CLR namespaces.
- Consume Cycles and Meals only through their application-level read capabilities.
- Own PDF rendering and its bounded, SSRF-protected image retrieval adapter in Infrastructure. Keep HTTP controllers and executable composition outside this module.
- Do not add Domain, Infrastructure, persistence, storage, or background-processing projects without proven Export-owned state or adapters.
- Preserve current-user access checks, sensitive-cycle password re-verification, range and item limits, cancellation, file names, content types, and export ordering/semantics.

## Tests

- Keep Export-owned application tests under `tests/FoodDiary.Modules.Export.Application.Tests`.
- Keep PDF rendering and remote-image transport tests under tests/FoodDiary.Modules.Export.Infrastructure.Tests. Keep HTTP, resources, host, architecture, and cross-module integration coverage in their existing projects.

## Verification

- Build: `dotnet build Modules/Export/Application/FoodDiary.Modules.Export.Application.csproj`
- Focused tests: `dotnet test Modules/Export/tests/FoodDiary.Modules.Export.Application.Tests/FoodDiary.Modules.Export.Application.Tests.csproj`
- Architecture: `dotnet test tests/FoodDiary.ArchitectureTests/FoodDiary.ArchitectureTests.csproj`
