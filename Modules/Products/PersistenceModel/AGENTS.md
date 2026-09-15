# Products PersistenceModel

Own ProductConfiguration and ApplyProductsPersistenceModel. Use canonical CLR names; preserve database object names, relationships, xmin, indexes and delete behavior. Shared context explicitly registers this assembly. Historical migrations/snapshot remain central.

ADR 0032 intentionally changes ImageAsset foreign keys to ClientNoAction (database NO ACTION): preserve saved image references during concurrent cleanup. Keep the matching shared migration and snapshot; other delete policies remain as declared.

Products extends the scalar persistence boundary to twenty-three models. Its three foreign FKs live in central ProductsCrossModuleRelationships: optional ImageAsset ClientNoAction, optional UsdaFood SetNull and the unchanged conventional User relationship. The owner model uses ID-only Images.Contracts and Users.Domain.Contracts; UsdaFdcId needs no foreign contract. Central Infrastructure references Usda.Domain directly. Preserve indexes, converters, xmin and ADR 0032 image integrity; no schema or API change is intended.

Current module convention: all projects use `FoodDiary.Modules.Products.<Project>` assembly identities and namespaces matching their folders, including tests. Preserve historical migration metadata and database/HTTP contracts during namespace moves.
