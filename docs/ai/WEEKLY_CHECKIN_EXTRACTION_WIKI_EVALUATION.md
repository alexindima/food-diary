# WeeklyCheckIn Extraction: LLM Wiki Evaluation

Date: 2026-08-30

## Scope

This evaluation records how the repository Wiki supported the physical extraction of WeeklyCheckIn from `FoodDiary.Application.WeeklyCheckIn` into `Modules/WeeklyCheckIn`.

## What the Wiki found correctly

- Classified the change as architectural/governed work and required research, design, ownership, dependency, architecture-health, and verification evidence.
- Identified TDEE, WeeklyGoals, and Hydration extraction commits as the closest precedents.
- After the TypeScript prerequisites were installed, grounded research in 26 current paths and reported a high-confidence runtime flow with 12 downstream/dependency entries.
- Ranked `WeeklyCheckInReadService`, the module read-service seam, and WeeklyGoals Contracts/Application among the most relevant sources.
- The full `test-plan` found 12 focused test files, 10 verification commands, and 13 risk scenarios, including dependency drift, stale implementation imports, contract consumers, API behavior, and source compatibility.
- `architecture-health -Check` accurately reported the final 341 production edges, 176 test edges, and no enforced drift.
- Affected generation used semantic no-op suppression, so only genuinely changed generated artifacts remained in the diff.

## What the Wiki missed or distorted

- The initial `start` ran without frontend TypeScript prerequisites and fell back to the read-only JSON baseline. That baseline produced zero focused tests and could not run ownership; after `npm ci`, the compiled graph found the expected test and consumer evidence.
- Before index regeneration, `test-plan` listed both the new module-owned tests and deleted donor paths. The subsequent `update` removed the stale donor evidence.
- The initial extraction acceptance matrix added five background-job criteria solely because the discovered paths included `FoodDiary.JobManager.csproj`. WeeklyCheckIn owns no job, and JobManager had only an unused project reference. The heuristic was corrected to require JobManager C# source or an explicit HostedService/Recurring path, with regression coverage in `Test-LlmWikiGovernedExtraction.ps1`.
- The generated acceptance matrix created before that fix remains historical task evidence; it should not be treated as authoritative over the corrected source, ADR, architecture tests, or final verification receipts.
- Source-impact review is intentionally approval-like: the first `verify` passed six stages but stopped until affected current pages were explicitly reviewed. Recording the evidence-based review allowed the resumable second run to complete 7/7 stages.

## Measured contribution

- Initial JSON-fallback `start`: useful for coarse scope and precedents, but low-confidence and materially incomplete.
- Compiled-graph research: 26 grounded paths, 12 dependency/runtime entries, and three strong extraction precedents.
- Test planning: 10 commands and 13 scenarios; the focused/module, consumer, architecture, solution-build, vulnerability, and Wiki checks were adopted.
- Full affected index update: 12 generators, completed in 197.1 seconds.
- Final resumable Wiki verification: 7/7 stages in 19.56 seconds after source-impact review.

Overall, the Wiki materially accelerated discovery and verification selection once its prerequisites and indexes were current. It was less reliable for generated acceptance criteria: those still require source-backed ownership review, particularly when composition projects appear only as project-reference consumers.
