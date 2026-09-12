# Users persistence model

Own Users aggregate and role/audit EF mappings only. Identity mappings for login
events and refresh-token sessions belong to Modules/Identity/Infrastructure/Model. Registration is explicit from
the shared `FoodDiaryDbContext`; migrations and snapshot stay central.

ADR 0032 intentionally changes ImageAsset foreign keys to ClientNoAction (database NO ACTION): preserve saved image references during concurrent cleanup. Keep the matching shared migration and snapshot; other delete policies remain as declared.

Use ID-only Images.Contracts for ImageAssetId. Central UsersCrossModuleRelationships owns the optional ProfileImageAssetId FK with ClientNoAction. Keep owned roles/goals, field access, converters and indexes local. Preserve explicit profile unlinking before image purge; no schema change.
