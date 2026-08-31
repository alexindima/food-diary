# Dashboard fixture path relocation audit

Parent authorized path-only fixture maintenance. Queries, IDs, cohorts, expected target counts and thresholds are unchanged; holdout-100 is byte-identical to base.

| Fixture | Old target | New target | Evidence |
| --- | --- | --- | --- |
| `.llm-wiki/evals/context-search-generalization.json` | `FoodDiary.Infrastructure/Persistence/Dashboard/DashboardStatisticsReadService.cs` | `Modules/Dashboard/Infrastructure/Persistence/Dashboard/DashboardStatisticsReadService.cs` | same C# after namespace/import relocation only |
| `.llm-wiki/evals/context-search-holdout.json` | `FoodDiary.Application.Dashboard/Queries/GetDashboardSnapshot/GetDashboardSnapshotQueryHandler.cs` | `Modules/Dashboard/Application/Queries/GetDashboardSnapshot/GetDashboardSnapshotQueryHandler.cs` | same C# content |
| `.llm-wiki/evals/context-search-holdout.json` | `FoodDiary.Infrastructure/Persistence/Dashboard/DashboardMealsReadService.cs` | `Modules/Dashboard/Infrastructure/Persistence/Dashboard/DashboardMealsReadService.cs` | same C# after namespace/import relocation only |
| `.llm-wiki/evals/context-search-probe-3.json` | `FoodDiary.Application.Dashboard/Services/DashboardStatisticsMapper.cs` | `Modules/Dashboard/Application/Services/DashboardStatisticsMapper.cs` | same C# content |
| `.llm-wiki/evals/context-search-probe-4.json` | `FoodDiary.Application.Dashboard/Services/DashboardSnapshotBuilder.cs` | `Modules/Dashboard/Application/Services/DashboardSnapshotBuilder.cs` | same C# content |
| `.llm-wiki/evals/context-search-probe-6.json` | `FoodDiary.Application.Dashboard/Services/DashboardSectionDataLoader.cs` | `Modules/Dashboard/Application/Services/DashboardSectionDataLoader.cs` | same C# content |
| `.llm-wiki/evals/context-search-probe-7.json` | `FoodDiary.Application.Dashboard/Internal/UtcDateNormalizer.cs` | `Modules/Dashboard/Application/Internal/UtcDateNormalizer.cs` | same C# content |
| `.llm-wiki/evals/context-search-unseen-20260826.json` | `FoodDiary.Application.Dashboard/Services/DashboardBodyMapper.cs` | `Modules/Dashboard/Application/Services/DashboardBodyMapper.cs` | same C# content |
| `.llm-wiki/evals/context-search-unseen-20260826.json` | `FoodDiary.Infrastructure/Persistence/Dashboard/DashboardMealProjection.cs` | `Modules/Dashboard/Infrastructure/Persistence/Dashboard/DashboardMealProjection.cs` | same C# after namespace/import relocation only |
| `.llm-wiki/evals/context-search.json` | `FoodDiary.Application.Dashboard/Queries/GetDashboardSnapshot/GetDashboardSnapshotQueryHandler.cs` | `Modules/Dashboard/Application/Queries/GetDashboardSnapshot/GetDashboardSnapshotQueryHandler.cs` | same C# content |
| `.llm-wiki/evals/development-context-bundles.json` | `FoodDiary.Infrastructure/Persistence/Dashboard/DashboardMealProjection.cs` | `Modules/Dashboard/Infrastructure/Persistence/Dashboard/DashboardMealProjection.cs` | same C# after namespace/import relocation only |
| `.llm-wiki/tools/Test-LlmWikiCodeGraph.ps1` | `FoodDiary.Application.Dashboard/` | `Modules/Dashboard/Application/` | existing physical project/root; legacy Application AssemblyName preserved |
| `.llm-wiki/tools/Test-LlmWikiGovernedExtraction.ps1` | `FoodDiary.Application.Dashboard/FoodDiary.Application.Dashboard.csproj` | `Modules/Dashboard/Application/FoodDiary.Modules.Dashboard.Application.csproj` | existing physical project/root; legacy Application AssemblyName preserved |
| `.llm-wiki/tools/Test-LlmWikiGovernedExtraction.ps1` | `FoodDiary.Application.Dashboard` | `Modules/Dashboard/Application` | existing physical project/root; legacy Application AssemblyName preserved |
| `.llm-wiki/tools/Test-LlmWikiGovernedExtraction.ps1` | `FoodDiary.Application.Abstractions/Dashboard` | `Modules/Dashboard/Application/Abstractions` | existing physical project/root; legacy Application AssemblyName preserved |

## Complete source audit

`dashboard-source-relocation-audit.json` records all 65 production source relocations with base, target and namespace-aligned SHA-256 hashes. Exactly 54 match after line-ending normalization; 11 infrastructure sources require only namespace/import alignment. All 65 aligned hashes match. This is not a claim that all files are byte-identical. Registration extraction is reviewed separately through the DI alias tests.

Frozen holdout-100 byte SHA-256 (base and current): `7b19546a9f7d98936ca4d86d8e5f8c9e511dea8eea56cc793ff60112fdb824b0`. Actual evaluation exits 1: top1 94/100, top10 98/100, errorCapture 0.5; the baseline failure is retained.
