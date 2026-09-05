---
id: generated.module.cycles
kind: module
status: current
generated_by: .llm-wiki/tools/Build-LlmWikiModulePages.ps1
sources:
  - .llm-wiki/tools/Build-LlmWikiModulePages.ps1
  - .llm-wiki/generated/repository-catalog.json
  - docs/architecture/module-dependencies.json
  - docs/architecture/backend-modules.json
---

# Cycles

## Graph

- Origin: extracted-project
- Extracted project: `Modules/Cycles/Application/FoodDiary.Application.Cycles.csproj`
- Business-module dependencies: none observed
- Abstraction-contract dependencies: Dashboard, Users
- Business-module consumers: none observed
- Host/adapter consumers: FoodDiary.Initializer, FoodDiary.Modules.Dashboard.Application, FoodDiary.Modules.Export.Application, FoodDiary.Web.Api
- Evidence model: compile-time namespaces plus project/composition source evidence; runtime DI/reflection may be incomplete.

## Source Areas

- `Modules/Cycles/Application`
- `Modules/Cycles/Application/Abstractions`
- `Modules/Cycles/Presentation`

## HTTP Surface

No literal attribute-routed controller was associated with this module.
## Boundary Health

- Role: aggregate-owner
- Physical isolation: project
- Architecture guardrails: project-reference-matrix
- Declared owned entities: CycleProfile, CycleFactor, CycleConsent, CycleSymptomEntry, CyclePredictionRevision, BleedingEntry, FertilitySignal
- Public contract files: 14
- Observed external consumer groups: 4
- Foreign repositories acquired: guarded where enforcement is explicit; otherwise not inferred from this page

## Public Surface

- Public contract types: 14
- Interfaces: 4
- DTO/read-model/projection types: 8
- Enums: 0
- Exported repository-shaped contracts: 4
- Contracts referencing domain entities: 2
- `class CycleDayErrors`
- `class CycleErrors`
- `interface ICycleReadModelRepository`
- `interface ICycleReadRepository`
- `interface ICycleRepository`
- `interface ICycleWriteRepository`
- `record BleedingEntryReadModel`
- `record CycleConsentReadModel`
- `record CycleFactorReadModel`
- `record CyclePredictionRevisionReadModel`
- `record CycleProfileReadModel`
- `record CycleSymptomEntryReadModel`
- `record FertilitySignalReadModel`
- `record MenstrualEpisodeReadModel`

## Focused Tests

Test paths below are discovery evidence, not proof that a boundary assertion executed or passed.

- [behavioral-or-text-match] `Modules/Cycles/tests/FoodDiary.Modules.Cycles.Application.Tests/Cycles/CyclesFeatureTests.ConsentAndConfirmation.cs`
- [behavioral-or-text-match] `Modules/Cycles/tests/FoodDiary.Modules.Cycles.Application.Tests/Cycles/CyclesFeatureTests.CreateAndRead.cs`
- [behavioral-or-text-match] `Modules/Cycles/tests/FoodDiary.Modules.Cycles.Application.Tests/Cycles/CyclesFeatureTests.DayCommands.cs`
- [behavioral-or-text-match] `Modules/Cycles/tests/FoodDiary.Modules.Cycles.Application.Tests/Cycles/CyclesFeatureTests.DeleteProfile.cs`
- [behavioral-or-text-match] `Modules/Cycles/tests/FoodDiary.Modules.Cycles.Application.Tests/Cycles/CyclesFeatureTests.EpisodeCommands.cs`
- [behavioral-or-text-match] `Modules/Cycles/tests/FoodDiary.Modules.Cycles.Application.Tests/Cycles/CyclesFeatureTests.EpisodeValidation.cs`
- [behavioral-or-text-match] `Modules/Cycles/tests/FoodDiary.Modules.Cycles.Application.Tests/Cycles/CyclesFeatureTests.FactorCommands.cs`
- [behavioral-or-text-match] `Modules/Cycles/tests/FoodDiary.Modules.Cycles.Application.Tests/Cycles/CyclesFeatureTests.MappingAndPrediction.cs`
- [behavioral-or-text-match] `Modules/Cycles/tests/FoodDiary.Modules.Cycles.Application.Tests/Cycles/CyclesFeatureTests.NutritionSummary.cs`
- [behavioral-or-text-match] `Modules/Cycles/tests/FoodDiary.Modules.Cycles.Application.Tests/Cycles/CyclesFeatureTests.SettingsCommands.cs`
- [behavioral-or-text-match] `Modules/Cycles/tests/FoodDiary.Modules.Cycles.Application.Tests/Cycles/CyclesFeatureTests.cs`
- [behavioral-or-text-match] `Modules/Cycles/tests/FoodDiary.Modules.Cycles.Application.Tests/Cycles/CyclesValidatorTests.cs`
- [behavioral-or-text-match] `Modules/Cycles/tests/FoodDiary.Modules.Cycles.Application.Tests/FeatureErrorContractTests.cs`
- [behavioral-or-text-match] `Modules/Cycles/tests/FoodDiary.Modules.Cycles.Application.Tests/Support/ResultAssert.cs`
- [behavioral-or-text-match] `Modules/Cycles/tests/FoodDiary.Modules.Cycles.Application.Tests/Time/CycleUtcDateNormalizerTests.cs`
- [behavioral-or-text-match] `Modules/Cycles/tests/FoodDiary.Modules.Cycles.Domain.Tests/Domain/CycleEnumContractTests.cs`
- [behavioral-or-text-match] `Modules/Cycles/tests/FoodDiary.Modules.Cycles.Domain.Tests/Domain/CycleIdConversionTests.cs`
- [behavioral-or-text-match] `Modules/Cycles/tests/FoodDiary.Modules.Cycles.Domain.Tests/Domain/CycleIdInvariantTests.cs`
- [behavioral-or-text-match] `Modules/Cycles/tests/FoodDiary.Modules.Cycles.Domain.Tests/Domain/CycleInternalOperationsTests.cs`
- [behavioral-or-text-match] `Modules/Cycles/tests/FoodDiary.Modules.Cycles.Domain.Tests/Domain/CycleProfileInvariantTests.cs`
- [integration] `Modules/Cycles/tests/FoodDiary.Modules.Cycles.Infrastructure.IntegrationTests/CycleRepositoryIntegrationTests.cs`
- [behavioral-or-text-match] `Modules/Cycles/tests/FoodDiary.Modules.Cycles.Infrastructure.Tests/CyclesModuleRegistrationTests.cs`
- [architecture-boundary] `tests/FoodDiary.ArchitectureTests/CyclesModuleExtractionTests.cs`
- [presentation] `tests/FoodDiary.Presentation.Api.Tests/CyclesControllerCoverageTests.cs`

## Working Rule

Use this page for discovery only. Read the nearest scoped `AGENTS.md` and
verify behavior in source code, tests, and API contract snapshots before
changing the module.
