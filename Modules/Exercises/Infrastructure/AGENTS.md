# Exercises Infrastructure

Own repository implementation and AddExercisesModule. Use central Infrastructure only for the shared context factory and transaction coordination; never move migrations or snapshot. Keep tracked mutations and shared transaction commit semantics. Persistence model sources compile in their separate project.

Runtime repositories receive only the owned ExerciseEntry DbSet from ExercisesDbContext. Register it through the shared CreateModuleContext factory and save through IUnitOfWork. Preserve tracked UpdateAsync semantics, user predicates and central migration/read/purge mappings (ADR 0040).
