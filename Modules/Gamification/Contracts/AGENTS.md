# Gamification consumer contracts

Own achievement administration inputs/projections and administration commands/queries consumed by Admin. Keep aggregate mutations and handlers in Application. Contracts depend on Mediator, Results and scalar Users.Domain.Contracts.

Preserve existing CLR namespaces and wire fields during coordinated rebuilds. Do not add persistence, provider clients, DI registration or handlers to this package. Reference the owning contract directly from every consumer.

IAchievementEvaluationOutbox is the consumer enqueue capability used by Lessons.
It belongs to Contracts; processing, repositories and reconciliation remain internal
Abstractions. Preserve ambient transaction and coalescing behavior in its adapter.

Use canonical FoodDiary.Modules.Gamification project identities and folder namespaces, including tests. Projects are siblings. Preserve historical migration metadata and relational schema.
