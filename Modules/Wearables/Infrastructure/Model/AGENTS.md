# Wearables Persistence Model Guidelines

- Own Wearables EF configurations and `ApplyWearablesPersistenceModel`.
- Preserve table, column, relationship, converter, index, CLR, and migration identities.
- The central `FoodDiaryDbContext`, migrations, and snapshot remain in `FoodDiary.Infrastructure`.

- Keep UserId conversions local through Users.Domain.Contracts. User Cascade relationships are composed by WearablesCrossModuleRelationships in central Infrastructure; this model must not reference foreign Domain assemblies.
