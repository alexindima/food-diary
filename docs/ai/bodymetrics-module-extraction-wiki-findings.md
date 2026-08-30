# BodyMetrics extraction: Wiki findings

## Useful navigation

- `start` captured clean baseline `037bbc254b`, selected critical workflow, initialized governed artifacts and produced a compatibility-oriented acceptance checklist.
- `research` surfaced recent Billing/Statistics extraction precedents and existing module boundary tests.
- `brief` and `test-plan` correctly identified database/architecture obligations and query-shape, validation, cancellation and concurrency review scenarios.
- The generated module page correctly identified the two logical entry feature groups, 18 public abstraction files and four host/adapter consumers.

## Reproducible limitations and false negatives

1. On a cold checkout without frontend TypeScript prerequisites, planning automatically uses the committed JSON baseline. `research` initially reported `relevant-workspace-paths=0`; its later seven grounded paths were broad caller-supplied paths, not a complete ownership inventory. This is navigation confidence, not implementation coverage.
2. `ownership -PlannedPath @('FoodDiary.Application.BodyMetrics','FoodDiary.Domain','FoodDiary.Infrastructure','Modules/BodyMetrics','tests','FoodDiary.slnx','docs')` returned no direct/downstream modules or scoped guides. Code confirms BodyMetrics and multiple projection consumers.
3. The generated BodyMetrics module page reported no HTTP surface and no business consumers; current Presentation WeightEntries/WaistEntries controllers, Dashboard, Statistics, TDEE and WeeklyCheckIn prove those seams exist.
4. Focused `test-plan` initially listed only BodyMetricsModuleExtractionTests, omitting four owned application test files, central entry/goal invariants, Dashboard/Users history-page coverage and persistence/HTTP tests. Test discovery must be checked against source references.
5. Focused health `privacy` returned 12 projection candidates, mostly Statistics/Users fields, and did not surface entry entities or user-scoped repository predicates. Its bounded output is not a complete health-data inventory.
6. `trace -Query CreateWeightEntryCommand` attempted a full graph build and failed because TypeScript was unavailable, despite backend-only intent. The diagnostic recommends an explicit JSON/backend-only route. No generator special case was added.
7. `delivery-replan` failed atomically with `compiled-index-projection-missing` after JSON-backed planning. Its facade does not pass through `CompiledIndexSource`; a SQLite graph build is required even when the prior planning steps explicitly used JSON.
8. After full `update` and `graph-build`, JSON-backed `trace -Query CreateWaistEntryCommand` still printed the removed legacy request/handler paths and both donor and relocated test paths. The refreshed generated C# and backend-contract indexes no longer contain the old handler path. Treat trace output as a navigation lead and verify filesystem existence; investigate facade snapshot/cache lineage generically rather than adding module-specific logic.
9. The refreshed module page reports zero public contracts after relocation, despite 18 real abstraction files. `Build-LlmWikiModulePages.ps1` unconditionally prefixes abstraction areas with `FoodDiary.Application.Abstractions/` (candidate discovery near line 190 and contract discovery near line 300), so repository-relative `Modules/.../Application/Abstractions` mappings become nonexistent paths. Billing/Hydration use the same manifest shape: this is a general path-resolution defect, not a BodyMetrics exception. The generator was not changed.

## False positives and classification cautions

- Goal types and goal mappings share weight/waist naming and the old BodyMetrics EF folder, but source ownership is the central User aggregate. Treating every name match as module-owned would create a cyclic Domain graph or change CLR/EF navigations.
- `start` generated migration/snapshot criteria for a persistence relocation; this extraction intentionally adds no migration. The relevant proof is unchanged EF model identity and a clean pending-model-change check.
- Calling brief/test-plan/decision/ownership without intent/planned paths before edits yields empty/no-diff context; that is a caller-scope issue, not evidence of low risk or isolation.
- Human-authored navigation pages retained hard-coded module totals from older extractions. This change replaces those stale totals with references to the generated current inventory.

## General recommendations

Keep graph evidence labels explicit; include contract consumers, literal presentation routes, and feature-name aliases in module discovery. Separate bounded search results from complete ownership claims. Backend traces should not require unrelated frontend prerequisites. None of these recommendations was implemented as a BodyMetrics-specific generator exception.
