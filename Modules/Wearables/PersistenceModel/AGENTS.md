# Wearables Persistence Model Guidelines

- Own Wearables EF configurations and `ApplyWearablesPersistenceModel`.
- Preserve table, column, relationship, converter, index and historical migration identities.
- The central `FoodDiaryDbContext`, migrations, and snapshot remain in `FoodDiary.Infrastructure`.

- Keep UserId conversions local through Users.Domain.Contracts. User Cascade relationships are composed by WearablesCrossModuleRelationships in central Infrastructure; this model must not reference foreign Domain assemblies.

All module projects and tests use `FoodDiary.Modules.Wearables.<Project>` identities and namespaces matching physical folders. Projects are siblings, including Application.Abstractions and PersistenceModel. Namespace changes preserve database schema, historical migration metadata, HTTP payloads and runtime behavior.
