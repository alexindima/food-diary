# Ai persistence model guidelines

Own the five Ai EF entity configurations and internal quota/job records. Preserve internal visibility via explicit friend assemblies. ApplyAiPersistenceModel is called by the central DbContext. Preserve schema, CLR names and migrations; quota records consume the existing reservation port and must not introduce a Domain-to-Application dependency.

The Ai PersistenceModel must not reference foreign Domain assemblies. Use Users.Domain.Contracts for UserId and the ID-only Images.Contracts for ImageAssetId. Keep scalar properties, indexes, conversions and same-owner relationships here. The four foreign relationships to User and ImageAsset are composed by FoodDiary.Infrastructure/Persistence/Composition/AiCrossModuleRelationships.cs after module model registration. Preserve their foreign keys and delete behavior; this separation requires no schema migration. See docs/ai/ai-persistence-boundary.md.
