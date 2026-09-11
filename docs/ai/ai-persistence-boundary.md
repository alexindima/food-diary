# AI persistence boundary pilot

AI PersistenceModel uses only its own Domain, its reservation port, Users.Domain.Contracts and the ID-only Images.Contracts. It no longer references Users.Domain or Images.Domain.

FoodDiary.Infrastructure/Persistence/Composition/AiCrossModuleRelationships.cs composes the three User foreign keys (AiUsage, AiQuotaPeriod, FoodRecognitionJob) and the FoodRecognitionJob image foreign key after all module model registrations. User links retain Cascade; the image link retains ClientNoAction. AI still owns entity declarations, scalar mappings, conversions, indexes, check constraints and same-owner relationships. Internal records remain internal using the existing friend assembly.

This is a bounded refinement of ADR 0031 for AI only. Other modules retain their existing typed foreign mappings. One database, DbContext and migration history remain; this is a compile-time boundary, not runtime or database isolation. Central Infrastructure directly references Ai.Domain for its consumed entity types. No public API or provider behavior changes.

Validation requires the exact project-reference matrix, a compiled-assembly test rejecting foreign Domain references in AI PersistenceModel, the existing no-pending-model-changes check, and PostgreSQL recognition/quota regression tests. No schema migration should be generated. Do not replace typed relationships with string-based entity names to evade dependencies.
