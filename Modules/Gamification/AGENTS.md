# Gamification Logical Module Guidelines

## Scope

Rules for `Modules/Gamification/`.

## Boundary

- Own achievement evaluation, awarding, reconciliation, administration, reads, achievement domain types, application ports, EF adapters, and owned EF configurations.
- Preserve the legacy `FoodDiary.Application.Gamification` assembly and existing CLR namespaces.
- Register the complete module through Infrastructure's `AddGamificationModule`; hosts remain composition roots.
- Keep `FoodDiaryDbContext`, historical migrations, snapshot, and generic claiming/retry/dead-letter replay central. Gamification's evaluation outbox record and EF mapping belong to its PersistenceModel project, using the shared Outbox.Abstractions contract. Preserve revision/coalescing and claim-release behavior.
- Read Meals, Dashboard, Users, and Lessons only through application-level capabilities.
- AchievementEvaluationOutboxReplayStream owns dead-letter queries and user-id preview metadata, registered once per scope through AddGamificationModule. The shared replay coordinator owns audit/reset/save/transaction; preserve revision and attempt-count semantics, and never save from the stream adapter.

## Tests

- Keep module-owned Application, Domain, and Infrastructure unit tests under `Modules/Gamification/tests/`.
- Keep HTTP, host, shared DbContext/integration, architecture, and cross-module tests central.

## Consumer boundary

Own achievement administration inputs/projections and IAchievementDefinitionAdministrationService consumed by Admin. Keep aggregate mutations and handlers in Application. Contracts depend only on Results. See `Contracts/AGENTS.md` and ADR 0033.
