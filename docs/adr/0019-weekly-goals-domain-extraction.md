# ADR 0019: Extract the WeeklyGoals Domain Project

- Status: Accepted
- Date: 2026-08-30
- Owners: Backend architecture
- Related: ADR 0016, ADR 0017
- Supersedes: ADR 0018

## Context

ADR 0018 deliberately kept `WeeklyGoal`, `WeeklyGoalId`, and `WeeklyGoalType` in central Domain until their CLR and EF dependencies could be proved safe. The follow-up analysis found no `User` aggregate navigation or cross-module domain ownership: `WeeklyGoal` stores `UserId`, and its EF relationship uses `HasOne<User>().WithMany()` without a CLR navigation. The shared context, historical migrations, and model snapshot identify the entity by its unchanged `FoodDiary.Domain.Entities.WeeklyGoals.WeeklyGoal` full name.

All source consumers are rebuilt together, the projects are non-packable, and the repository contains no assembly-qualified reflection or serialization dependency on these WeeklyGoals types. The established Fasting pattern already permits a module Domain project to reference central Domain one-way while shared `User` and `UserId` ownership remains centralized.

## Decision Drivers

- Make WeeklyGoals domain ownership explicit without creating a `User` aggregate cycle.
- Preserve CLR namespaces and EF model identity so no schema or migration change is produced.
- Keep the shared context and migration history centralized.
- Preserve API, application, reminder, repository, and transaction behavior.

## Considered Options

1. Retain the three types in central Domain as the ADR 0018 compatibility seam.
2. Extract them into a WeeklyGoals Domain project that references central Domain one-way.
3. Move `User` or introduce a WeeklyGoals navigation on `User` as part of the extraction.

## Decision

Create `Modules/WeeklyGoals/Domain/FoodDiary.Modules.WeeklyGoals.Domain.csproj` and move `WeeklyGoal`, `WeeklyGoalId`, and `WeeklyGoalType` into it while preserving their existing `FoodDiary.Domain.*` namespaces. The module Domain project references central Domain only for shared `UserId` compatibility and must not reference application, persistence, transport, or host projects.

Do not add a `User` navigation, change the EF mapping, or move `FoodDiaryDbContext`, historical migrations, or the model snapshot. Application abstractions, application behavior, the WeeklyGoals persistence model, and central Infrastructure reference the new Domain assembly explicitly. Move only the two dedicated WeeklyGoals domain test files into a module Domain test project; central cross-module tests remain in their current projects.

The new assembly identity is intentionally not compatible with stale precompiled binaries that referenced these types from `FoodDiary.Domain`. This repository has no supported external binary consumer or packable artifact for those types; all in-repository consumers are rebuilt in one solution.

## Consequences

### Positive

- WeeklyGoals owns its aggregate, identifier, enum, invariants, and dedicated tests under one logical root.
- The project graph remains acyclic and matches the proven Fasting extraction pattern.
- EF full-name identity, tables, columns, indexes, relationship, cascade behavior, HTTP surface, and reminder behavior remain unchanged.

### Negative

- The module Domain remains coupled to central Domain until shared `UserId` ownership is reconsidered.
- A separately distributed binary compiled against the former assembly identity would require recompilation.

## Enforcement

- `WeeklyGoalsModuleExtractionTests` verifies exclusive source ownership and the allowed Domain dependency.
- `ProjectDependencyMatrixTests` governs the new production and test projects and every changed project edge.
- EF pending-model checks and the unchanged migration snapshot prove that no migration is required.
- Module and donor domain-test counts prove that the two dedicated test files moved without lost coverage.

## Follow-up

None.
