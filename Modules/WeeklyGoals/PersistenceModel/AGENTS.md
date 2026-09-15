# Weekly Goals Persistence Model Guidelines

WeeklyGoals EF configuration and the model-builder registration seam live here. Reference the WeeklyGoals Domain project and preserve table, columns, indexes, relationships, conversions, cascade behavior, and the `FoodDiary.Modules.WeeklyGoals.Domain.Entities.WeeklyGoal` EF model identity. Do not reference central Infrastructure or application projects.

The foreign User relationship is composed by `FoodDiary.Infrastructure/Persistence/Composition/WeeklyGoalsCrossModuleRelationships.cs` after owned models are registered. Keep owned scalar mappings, indexes and conversions here, and preserve Cascade without a schema migration. Foreign Domain assembly references are prohibited; reference Users.Domain.Contracts directly for UserId. See docs/ai/scalar-persistence-boundaries.md.

All module projects and tests use `FoodDiary.Modules.WeeklyGoals.<Project>` identities and namespaces matching physical folders. Projects are siblings, including Application.Abstractions and PersistenceModel. Namespace changes preserve database schema, historical migration metadata, HTTP payloads and runtime behavior.
