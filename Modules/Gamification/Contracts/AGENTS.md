# Gamification consumer contracts

Own achievement administration inputs/projections and IAchievementDefinitionAdministrationService consumed by Admin. Keep aggregate mutations and handlers in Application. Contracts depend only on Results.

Preserve existing CLR namespaces and wire fields during coordinated rebuilds. Do not add persistence, provider clients, DI registration or handlers to this package. Reference the owning contract directly from every consumer.
