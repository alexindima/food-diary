# Exercises Persistence Model

Own ExerciseEntryConfiguration and ApplyExercisesPersistenceModel. Preserve CLR entity name, date column, enum string conversion, indexes and User cascade relationship. Never depend on shared Infrastructure or add a DbContext.

The foreign User relationship is composed by `FoodDiary.Infrastructure/Persistence/Composition/ExercisesCrossModuleRelationships.cs` after owned models are registered. Keep owned scalar mappings, indexes and conversions here, and preserve Cascade without a schema migration. Foreign Domain assembly references are prohibited; reference Users.Domain.Contracts directly for UserId. See docs/ai/scalar-persistence-boundaries.md.
