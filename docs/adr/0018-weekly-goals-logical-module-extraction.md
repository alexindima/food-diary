# ADR 0018: WeeklyGoals Logical Module Extraction

- Status: Superseded
- Date: 2026-08-29
- Owners: Backend architecture
- Related: ADR 0015, ADR 0016, ADR 0017
- Superseded by: ADR 0019

## Context

WeeklyGoals application behavior, ports, repositories, EF mapping, API consumers, and reminder scheduling were spread across horizontal projects. A vertical extraction must improve ownership without changing the public HTTP contract, database model, existing CLR type identity, advisory-lock semantics, or the recurring reminder behavior.

`WeeklyGoal`, `WeeklyGoalId`, and `WeeklyGoalType` are public central-Domain types referenced by application ports, tests, central `FoodDiaryDbContext`, and the compiled EF migration snapshot. Moving them to another assembly would be a binary CLR compatibility break even if namespaces remained unchanged.

## Decision

- `Modules/WeeklyGoals` is the canonical logical root.
- `Application/FoodDiary.Modules.WeeklyGoals.Application.csproj` owns application behavior as a semantic Application project and retains assembly name `FoodDiary.Application.WeeklyGoals` plus legacy application namespaces.
- `Application/Abstractions` owns repository and serialized-transaction ports while preserving their legacy namespaces.
- `Contracts` owns the weekly-goal read model and read-service contract while preserving their legacy namespaces.
- `Infrastructure` owns repository implementations, the advisory-lock transaction runner, and the complete `AddWeeklyGoalsModule` facade.
- `Infrastructure/Model` owns the unchanged EF configuration and exposes `ApplyWeeklyGoalsPersistenceModel` to the shared context.
- `WeeklyGoal`, `WeeklyGoalId`, and `WeeklyGoalType` remain in central Domain as an explicit compatibility seam.
- `FoodDiaryDbContext`, all historical migrations, and the model snapshot remain in central Infrastructure.
- HTTP routes, payloads, status codes, OpenAPI, tables, columns, indexes, conversions, cascades, job ID, cron/options binding, batching, cancellation, retry behavior, and reminder processing semantics remain unchanged.
- Executable hosts reference WeeklyGoals Infrastructure for composition. JobManager also references the application assembly because its scheduler adapter directly invokes `WeeklyGoalReminderProcessor`.

## Consequences

WeeklyGoals gains one discoverable vertical root and central Infrastructure no longer owns its repositories or configuration. The central Domain and migration host remain horizontal by design. A future Domain move requires a separately governed binary-compatibility and EF migration decision; this ADR and architecture tests intentionally prevent it from happening as incidental cleanup.

## Enforcement

- `WeeklyGoalsModuleExtractionTests` forbids legacy application and central persistence source, verifies the central Domain seam, and checks composition roots.
- `ProjectDependencyMatrixTests` governs every new project edge.
- `docs/architecture/backend-modules.json` declares the logical root and compatibility mappings.
- Provider-backed integration tests and EF pending-model checks prove that query, concurrency, and model behavior remain unchanged.
