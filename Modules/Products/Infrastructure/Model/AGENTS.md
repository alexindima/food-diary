# Products Infrastructure/Model

Own ProductConfiguration and ApplyProductsPersistenceModel. Preserve all CLR/EF names, relationships, xmin, indexes and delete behavior. Shared context explicitly registers this assembly. Historical migrations/snapshot remain central.

ADR 0032 intentionally changes ImageAsset foreign keys to ClientNoAction (database NO ACTION): preserve saved image references during concurrent cleanup. Keep the matching shared migration and snapshot; other delete policies remain as declared.
