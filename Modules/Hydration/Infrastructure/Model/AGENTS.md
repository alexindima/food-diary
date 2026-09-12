# Hydration Persistence Model Guidelines

Hydration EF configurations and the model-builder registration seam live here. Preserve tables, columns, indexes, conversions and the User FK/cascade relationship through central typed relationship composition. The domain stores only UserId; neither side has a CLR navigation. Users.Domain.Contracts supplies UserId; Users.Domain must not be referenced by this model.

Do not reference central Infrastructure/application projects or add a DbContext. Navigation metadata changes require snapshot and PostgreSQL validation; unchanged relational schema does not need an empty migration. See ADR 0029.

HydrationOperationReceipt owns a composite UserId/OperationId key and unique EntryId. Deliberately omit an entry FK so deletion does not erase replay protection. Its User cascade FK is composed centrally alongside HydrationEntry; migration and PostgreSQL tests cover account purge.

The foreign User relationship is composed by `FoodDiary.Infrastructure/Persistence/Composition/HydrationCrossModuleRelationships.cs` after owned models are registered. Keep owned scalar mappings, indexes and conversions here, and preserve Cascade without a schema migration. Foreign Domain assembly references are prohibited; reference Users.Domain.Contracts directly for UserId. See docs/ai/scalar-persistence-boundaries.md.
