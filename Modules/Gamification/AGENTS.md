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

Both Revision and LockedBy fence every EF finalization, including failures. On a conflict, release only a changed revision still held by the original owner; preserve the new request timestamp, attempt count and failure metadata. Shared processing owns SaveChanges and durable-outcome logging.

## Scalar persistence boundary

PersistenceModel uses Users.Domain.Contracts for UserId. Its foreign User Cascade
relationship is composed by GamificationCrossModuleRelationships in central
Infrastructure after owned models. Keep local mappings and same-owner relationships
unchanged; do not restore a Users.Domain dependency to the model.

Application consumes scalar Users types through Users.Domain.Contracts and semantic
capabilities through Users.Contracts. Do not reference the aggregate-bearing
Users.Domain assembly for these types.

Scalar types AchievementMetric belong to Domain.Contracts.
Reference that owner directly without acquiring aggregate capabilities.

AchievementDefinitionLimits owns shared length limits. AchievementDefinition retains constant aliases for compatibility; Admin validators consume the narrow limits directly.
