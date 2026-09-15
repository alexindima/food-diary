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
- Extracted project: `Modules/Cycles/Application/FoodDiary.Modules.Cycles.Application.csproj`
- Business-module dependencies: none observed
- Abstraction-contract dependencies: Meals, Users
- Business-module consumers: none observed
- Host/adapter consumers: FoodDiary.Initializer, FoodDiary.Web.Api
- Evidence model: compile-time namespaces plus project/composition source evidence; runtime DI/reflection may be incomplete.

## Source Areas

- `Modules/Cycles/Application`
- `Modules/Cycles/Application.Abstractions`
- `Modules/Cycles/Contracts`
- `Modules/Cycles/Presentation`

## HTTP Surface

No literal attribute-routed controller was associated with this module.
## Boundary Health

- Role: aggregate-owner
- Physical isolation: project
- Architecture guardrails: project-reference-matrix
- Declared owned entities: CycleProfile, CycleFactor, CycleConsent, CycleSymptomEntry, CyclePredictionRevision, BleedingEntry, FertilitySignal
- Public contract files: 23
- Observed external consumer groups: 2
- Foreign repositories acquired: guarded where enforcement is explicit; otherwise not inferred from this page

## Public Surface

- Public contract types: 23
- Interfaces: 2
- DTO/read-model/projection types: 19
- Enums: 0
- Exported repository-shaped contracts: 2
- Contracts referencing domain entities: 0
- `class CycleErrors`
- `interface ICycleReadModelRepository`
- `interface ICycleWriteRepository`
- `record BleedingEntryModel`
- `record BleedingEntryReadModel`
- `record CycleConsentModel`
- `record CycleConsentReadModel`
- `record CycleFactorModel`
- `record CycleFactorReadModel`
- `record CycleLogDayModel`
- `record CycleModel`
- `record CycleNutritionSummaryModel`
- `record CyclePredictionRevisionModel`
- `record CyclePredictionRevisionReadModel`
- `record CyclePredictionsModel`
- `record CycleProfileReadModel`
- `record CycleSymptomEntryModel`
- `record CycleSymptomEntryReadModel`
- `record FertilitySignalModel`
- `record FertilitySignalReadModel`
- `record GetCurrentCycleQuery`
- `record MenstrualEpisodeModel`
- `record MenstrualEpisodeReadModel`

## Focused Tests

Test paths below are discovery evidence, not proof that a boundary assertion executed or passed.

- [behavioral-or-text-match] `Modules/Cycles/tests/FoodDiary.Modules.Cycles.Application.Tests/CyclesFeatureTests.ConsentAndConfirmation.cs`
- [behavioral-or-text-match] `Modules/Cycles/tests/FoodDiary.Modules.Cycles.Application.Tests/CyclesFeatureTests.CreateAndRead.cs`
- [behavioral-or-text-match] `Modules/Cycles/tests/FoodDiary.Modules.Cycles.Application.Tests/CyclesFeatureTests.DayCommands.cs`
- [behavioral-or-text-match] `Modules/Cycles/tests/FoodDiary.Modules.Cycles.Application.Tests/CyclesFeatureTests.DeleteProfile.cs`
- [behavioral-or-text-match] `Modules/Cycles/tests/FoodDiary.Modules.Cycles.Application.Tests/CyclesFeatureTests.EpisodeCommands.cs`
- [behavioral-or-text-match] `Modules/Cycles/tests/FoodDiary.Modules.Cycles.Application.Tests/CyclesFeatureTests.EpisodeValidation.cs`
- [behavioral-or-text-match] `Modules/Cycles/tests/FoodDiary.Modules.Cycles.Application.Tests/CyclesFeatureTests.FactorCommands.cs`
- [behavioral-or-text-match] `Modules/Cycles/tests/FoodDiary.Modules.Cycles.Application.Tests/CyclesFeatureTests.MappingAndPrediction.cs`
- [behavioral-or-text-match] `Modules/Cycles/tests/FoodDiary.Modules.Cycles.Application.Tests/CyclesFeatureTests.NutritionSummary.cs`
- [behavioral-or-text-match] `Modules/Cycles/tests/FoodDiary.Modules.Cycles.Application.Tests/CyclesFeatureTests.SettingsCommands.cs`
- [behavioral-or-text-match] `Modules/Cycles/tests/FoodDiary.Modules.Cycles.Application.Tests/CyclesFeatureTests.cs`
- [behavioral-or-text-match] `Modules/Cycles/tests/FoodDiary.Modules.Cycles.Application.Tests/CyclesValidatorTests.cs`
- [behavioral-or-text-match] `Modules/Cycles/tests/FoodDiary.Modules.Cycles.Application.Tests/FeatureErrorContractTests.cs`
- [behavioral-or-text-match] `Modules/Cycles/tests/FoodDiary.Modules.Cycles.Application.Tests/Support/ResultAssert.cs`
- [behavioral-or-text-match] `Modules/Cycles/tests/FoodDiary.Modules.Cycles.Application.Tests/Time/CycleUtcDateNormalizerTests.cs`
- [behavioral-or-text-match] `Modules/Cycles/tests/FoodDiary.Modules.Cycles.Domain.Tests/CycleAdditionalInvariantTests.cs`
- [behavioral-or-text-match] `Modules/Cycles/tests/FoodDiary.Modules.Cycles.Domain.Tests/CycleEnumContractTests.cs`
- [behavioral-or-text-match] `Modules/Cycles/tests/FoodDiary.Modules.Cycles.Domain.Tests/CycleIdConversionTests.cs`
- [behavioral-or-text-match] `Modules/Cycles/tests/FoodDiary.Modules.Cycles.Domain.Tests/CycleIdInvariantTests.cs`
- [behavioral-or-text-match] `Modules/Cycles/tests/FoodDiary.Modules.Cycles.Domain.Tests/CycleInternalOperationsTests.cs`
- [behavioral-or-text-match] `Modules/Cycles/tests/FoodDiary.Modules.Cycles.Domain.Tests/CycleProfileInvariantTests.cs`
- [behavioral-or-text-match] `Modules/Cycles/tests/FoodDiary.Modules.Cycles.Domain.Tests/CycleSettingsAtomicityTests.cs`
- [integration] `Modules/Cycles/tests/FoodDiary.Modules.Cycles.Infrastructure.IntegrationTests/CycleRepositoryIntegrationTests.cs`
- [behavioral-or-text-match] `Modules/Cycles/tests/FoodDiary.Modules.Cycles.Infrastructure.Tests/CyclesModuleRegistrationTests.cs`
- [presentation] `Modules/Cycles/tests/FoodDiary.Modules.Cycles.Presentation.Tests/CycleHttpMappingsTests.cs`
- [presentation] `Modules/Cycles/tests/FoodDiary.Modules.Cycles.Presentation.Tests/CyclesControllerCoverageTests.cs`
- [architecture-boundary] `tests/FoodDiary.ArchitectureTests/CyclesModuleExtractionTests.cs`
- [integration] `tests/FoodDiary.Infrastructure.IntegrationTests/Integration/CyclesContextCompositionIntegrationTests.cs`

## Working Rule

Use this page for discovery only. Read the nearest scoped `AGENTS.md` and
verify behavior in source code, tests, and API contract snapshots before
changing the module.
