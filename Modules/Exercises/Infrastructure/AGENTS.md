# Exercises Infrastructure

Own repository implementation and AddExercisesModule. Consume IModuleContextFactory from FoodDiary.Persistence.Abstractions and IUnitOfWork from application contracts; do not reference central Infrastructure; never move migrations or snapshot. Keep tracked mutations and shared transaction commit semantics. Persistence model sources compile in their separate project.

Runtime repositories receive only the owned ExerciseEntry DbSet from ExercisesDbContext. Register it through the shared CreateModuleContext factory and save through IUnitOfWork. Preserve tracked UpdateAsync semantics, user predicates and central migration/read/purge mappings (ADR 0040).

Use the canonical project name as the namespace root and match folders. Projects are siblings. Public owner use cases are Contracts requests dispatched through ISender; keep outbound source ports and reusable algorithms separate. Preserve authorization, cancellation, wire shapes and persistence semantics.
