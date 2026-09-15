# RecipeCommunity Domain

Own RecipeComment, RecipeLike and their IDs. Preserve scalar UserId/RecipeId relationships and invariants; central Domain must not reference this project.

User ownership: reference Users Domain.Contracts for UserId and shared user values. Keep foreign keys scalar; foreign aggregate CLR navigations are prohibited. PersistenceModel preserves the relational constraints with typed HasOne<T>() mappings.

Current module convention: all projects use `FoodDiary.Modules.RecipeCommunity.<Project>` assembly identities and namespaces matching their folders, including tests. Preserve historical migration metadata and database/HTTP contracts during namespace moves.
