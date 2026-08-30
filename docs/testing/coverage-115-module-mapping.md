# Coverage baseline 115: module-owned regression mapping

## Scope and evidence

Input: `C:\Users\alexi\Downloads\115.json`, dotCover 2026.2.0.2.
SHA-256: `8ACD98A93F67829998065EC6DB3103AD2F247AE036116A50BB363AEFC1DF2BB3`.

The input was parsed as data. Every non-leaf node's covered/total statement counts equal the sums of its children. Counts below count project/leaf nodes once, never both parents and descendants.

| Scope | Covered | Total | Gaps |
| --- | ---: | ---: | ---: |
| Whole baseline | 42,318 | 42,571 | 253 |
| JSON solution folder named Modules | 9,622 | 9,698 | 76 |
| Current physical production projects under Modules/** | 10,674 | 10,759 | 85 |

The physical scope is 75 baseline projects, resolved against current production `.csproj` file names and explicit `AssemblyName` values. It includes Billing (690/696), Export (270/273), and Statistics (92/92), which the baseline places outside its Modules solution folder. Test projects were excluded from resolution.

Important limitation: this JSON contains symbol-level counts but **no source paths, statement IDs, source ranges, or per-line hit data**. Therefore an exact measured list of uncovered line numbers cannot be recovered from this input. The complete symbol/count inventory below is exact; source branches for partially covered methods are source-reviewed candidates, not invented measured line hits. The user's next detailed coverage run is the final line-level verification. No coverage collector was run during this task.

## Complete gap-to-test mapping

Production paths below are relative to `Modules/`. All tests live in the corresponding `Modules/<module>/tests` project.

| Production source / baseline member | Gaps | Behavioral evidence |
| --- | ---: | --- |
| `Billing/Application/Commands/ProcessBillingWebhook/BillingWebhookPaymentRecorder.cs`: `AddIfPresentAsync` | 4 | `BillingFeatureTests.BaselineRegressionTests.cs`: existing payment receives changed provider amount, status, kind, event ID without adding a duplicate. |
| `Billing/Application/Services/BillingAccessService.cs`: `EnsurePremiumRoleAsync` | 2 | Same test file: externally granted unmanaged premium is preserved for both requested premium states, with no role or subscription writes. |
| `Export/Application/Services/CsvFieldEscaper.cs`: `NeutralizeSpreadsheetFormula` | 3 | `CsvFieldEscaperTests`: whitespace/control prefixes before formula markers, ordinary text, and all-whitespace input. Asserts exact output, preserving original prefix. |
| `ContentReports/Infrastructure/ModuleRegistration.cs`: `IContentReportTargetReadService` factory | 1 | `ContentReportsInfrastructureTests`: resolve all aliases and assert same scoped owned repository. |
| `Cycles/Domain/ValueObjects/Ids/BleedingEntryId.cs`: explicit Guid conversion, implicit Guid conversion, ToString | 3 | `CycleIdConversionTests`: exact Guid round-trip and exact formatting. |
| `Cycles/Domain/ValueObjects/Ids/CycleConsentId.cs`: Empty, explicit/implicit Guid conversions, ToString | 4 | `CycleIdConversionTests`: empty identity plus exact round-trip/format. |
| `Cycles/Domain/ValueObjects/Ids/CycleFactorId.cs`: explicit/implicit Guid conversions, ToString | 3 | `CycleIdConversionTests`. |
| `Cycles/Domain/ValueObjects/Ids/CyclePredictionRevisionId.cs`: explicit/implicit Guid conversions, ToString | 3 | `CycleIdConversionTests`. |
| `Cycles/Domain/ValueObjects/Ids/CycleProfileId.cs`: explicit/implicit Guid conversions, ToString | 3 | `CycleIdConversionTests`. |
| `Cycles/Domain/ValueObjects/Ids/CycleSymptomEntryId.cs`: explicit/implicit Guid conversions, ToString | 3 | `CycleIdConversionTests`. |
| `Cycles/Domain/ValueObjects/Ids/FertilitySignalId.cs`: explicit/implicit Guid conversions, ToString | 3 | `CycleIdConversionTests`. |
| `Cycles/Domain/ValueObjects/Ids/MenstrualEpisodeId.cs`: explicit/implicit Guid conversions, ToString | 3 | `CycleIdConversionTests`. |
| `Cycles/Infrastructure/ModuleRegistration.cs`: `ICycleReadRepository` factory | 1 | `CyclesModuleRegistrationTests`: actual resolution and alias identity. |
| `Favorites/Domain/ValueObjects/Ids/FavoriteMealId.cs`: Empty, explicit/implicit Guid conversions, ToString | 4 | `FavoriteIdConversionTests`: exact value/format and empty identity. |
| `Favorites/Domain/ValueObjects/Ids/FavoriteProductId.cs`: explicit/implicit Guid conversions, ToString | 3 | `FavoriteIdConversionTests`. |
| `Favorites/Domain/ValueObjects/Ids/FavoriteRecipeId.cs`: explicit/implicit Guid conversions, ToString | 3 | `FavoriteIdConversionTests`. |
| `Gamification/Domain/ValueObjects/Ids/AchievementDefinitionId.cs`: Empty, explicit/implicit Guid conversions, ToString | 4 | `GamificationIdConversionTests`. |
| `Gamification/Domain/ValueObjects/Ids/UserAchievementId.cs`: explicit/implicit Guid conversions, ToString | 3 | `GamificationIdConversionTests`. |
| `Gamification/Infrastructure/Persistence/AchievementEvaluationOutboxProcessor.cs`: `TryMarkProcessedAsync` | 3 | `AchievementPersistenceTests`: a second context requests a new revision during reconciliation; one dispatch leaves revision 2 pending and releases its claim. |
| `Images/Application/Commands/ConfirmUpload/ConfirmImageUploadCommandHandler.cs`: `Handle` | 1 | `ImagesFeatureTests`: storage-originated cancellation without caller cancellation maps to StorageError; existing tests cover caller cancellation, persistence rollback, rollback failure, invalid and idempotent uploads. |
| `Images/Application/Abstractions/Common/IImageObjectDeletionOutbox.cs`: default `EnqueueAsync(string, CancellationToken)` | 1 | `ImagesFeatureTests.ImageAbstraction_DefaultOverloads_ForwardSemanticDefaults`: confirms object key and `isConfirmed=true` forwarding. |
| `Images/Application/Abstractions/Common/IImageStorageService.cs`: default `ConfirmUploadedObjectAsync` | 1 | Same test: forwards to validation and preserves the validation result. |
| `Marketing/Application/Commands/RecordMarketingAttribution/RecordMarketingAttributionCommandHandler.cs`: `Handle` | 4 | `MarketingAttributionCoverageTests`: duplicate signup is a no-op without landing lookup; absent trusted landing is a successful no-op after one lookup. |
| `Marketing/Domain/ValueObjects/Ids/MarketingAttributionEventId.cs`: Empty, explicit/implicit Guid conversions, ToString | 4 | `MarketingIdConversionTests`. |
| `OpenFoodFacts/Infrastructure/ModuleRegistration.cs`: read and write cache-repository factories | 2 | `ModuleRegistrationTests`: both aliases resolve to the same owned scoped repository. |
| `Wearables/Application/Commands/SyncWearableData/SyncWearableDataCommandHandler.cs`: `ProtectLegacyTokens` | 1 | `LegacyTokenUpgradeTests`: persisted legacy access token without refresh token is upgraded after successful provider fetch. EF's property API establishes legacy persisted state; no reflection/dynamic is used. |
| `Wearables/Domain/Entities/Wearables/WearableConnection.cs`: non-nullable `EnsureProtected` | 2 | `WearableInvariantTests`: default/empty and non-empty legacy raw tokens are rejected by the persistence guard with exact parameter name. |
| `Wearables/Domain/ValueObjects/Ids/WearableConnectionId.cs`: Empty, explicit/implicit Guid conversions, ToString | 4 | `WearableIdConversionTests`. |
| `Wearables/Domain/ValueObjects/Ids/WearableSyncEntryId.cs`: Empty, explicit/implicit Guid conversions, ToString | 4 | `WearableIdConversionTests`. |
| `Wearables/Infrastructure/ModuleRegistration.cs`: connection read/write and sync read/read-model/write factories | 5 | `ModuleRegistrationTests`: all five aliases resolve to their correct scoped repositories. |
| **Total** | **85** | No baseline gap omitted from the symbol inventory. |

## Production changes and exclusions

No production coverage exclusions and no runtime behavior changes. The only production edit grants the module-owned Wearables Infrastructure test assembly internal visibility, matching the existing visibility for its Infrastructure integration tests. It allows construction of the existing persisted-token representation without reflection or a new public runtime API.

Three Infrastructure test projects add the already centrally-versioned EF InMemory test dependency for actual DI repository resolution and legacy persisted-state testing. InMemory tests do not replace existing PostgreSQL integration tests.

## Wiki usefulness and limitations

- Root/scoped guides and index located the correct module owners and preserved legacy namespace/assembly constraints.
- `brief` identified architecture, cancellation, concurrency and ownership obligations. It also suggested unrelated frontend tests through downstream graph expansion; these were treated as contextual false positives for this test-only change.
- `coverage-plan` correctly derived focused test commands after receiving an exact test file. Its collector commands were deliberately not executed, as requested.
- `research` and `test-plan` were invoked with explicit paths. `test-plan` with exact module test directories selected all 14 changed test projects. A first cold call was stopped after prolonged silence; the subsequent explicit JSON-index route completed. No generator changes were made.
- `research` classified the billing-source context as critical and requested a design checkpoint about boundary preservation. For this test-only change that is an overbroad readiness warning: the existing boundary is unchanged, as required by the user, and the payment recorder is exercised directly without changing runtime ownership or behavior.
- Initial manual classification by the JSON solution-folder label missed Billing/Export; physical project/AssemblyName resolution corrected the inventory from 76 to 85. This was an audit correction, not a Wiki generator defect.
- The two Images interface members are executable default implementations, not passive declarations. They are tested, not excluded.
- A cold `dotnet test --no-restore` with a new artifacts path returned success without test execution. Those invocations are not evidence. Final results below require actual VSTest summaries after a restored solution build.

## Verification and remaining risk

Ordinary module test runs (no collector): Cycles Domain 86, Cycles Infrastructure 1, Favorites Domain 18, Gamification Domain 16, Gamification Infrastructure 6, Images Application 40, Marketing Application 18, Marketing Domain 3, OpenFoodFacts Infrastructure 1, Wearables Application consumer suite 46, Wearables Domain 34, Wearables Infrastructure 17, ContentReports Infrastructure 2, Billing Application 113, Export Application 66. All passed.

Full ArchitectureTests: 742 passed. Final full solution build with repository-level `.artifacts/coverage115-build` passed with zero warnings and zero errors. `wiki update -AffectedOnly -Verify` completed all 7 verification stages; generated artifacts are included. `git diff --check` passed. Commit hooks remain enabled.

Expected next measurement: all 85 targeted symbol-level statement gaps should close, subject to detailed report verification of the partially covered async methods. 100% line coverage is not claimed as measured: the supplied JSON has no line data, and the user owns the final measurement. No push is performed.
