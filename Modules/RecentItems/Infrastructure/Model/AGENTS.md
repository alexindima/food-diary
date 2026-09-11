# RecentItems Persistence Model

Own `RecentItemConfiguration` and its explicit registration extension. Preserve columns, conversions, indexes, one-way User foreign key and cascade delete exactly. Do not add a migration without an EF model delta.

The foreign User relationship is composed by `FoodDiary.Infrastructure/Persistence/Composition/RecentItemsCrossModuleRelationships.cs` after owned models are registered. Keep owned scalar mappings, indexes and conversions here, and preserve Cascade without a schema migration. Foreign Domain assembly references are prohibited; reference Users.Domain.Contracts directly for UserId. See docs/ai/scalar-persistence-boundaries.md.
