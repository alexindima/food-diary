# Users persistence model

UserPreferences and UserNutritionProfiles are independent owner tables with xmin,
audit timestamps and cascading account foreign keys. User forwarding properties
are ignored by EF; SQL uses the mapped navigations. The paired central split
migration copies all legacy values before dropping columns and restores latest
values on Down. Releases require stopped writers and compatible binaries (ADR 0052).

Own Users aggregate and role/audit EF mappings only. Identity mappings for login
events and refresh-token sessions belong to Modules/Identity/PersistenceModel. Registration is explicit from
the shared `FoodDiaryDbContext`; migrations and snapshot stay central.

ADR 0032 intentionally changes ImageAsset foreign keys to ClientNoAction (database NO ACTION): preserve saved image references during concurrent cleanup. Keep the matching shared migration and snapshot; other delete policies remain as declared.

Use ID-only Images.Contracts for ImageAssetId. Central UsersCrossModuleRelationships owns the optional ProfileImageAssetId FK with ClientNoAction. Keep owned roles/goals, field access, converters and indexes local. Preserve explicit profile unlinking before image purge; no schema change.

All module projects and tests use `FoodDiary.Modules.Users.<Project>` identities and namespaces matching physical folders. Projects are siblings, including Application.Abstractions and PersistenceModel. Namespace changes preserve database schema, historical migration metadata, HTTP payloads and runtime behavior.
