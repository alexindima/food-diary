# RecipeCommunity Domain

Own RecipeComment, RecipeLike and their IDs. Preserve namespaces, scalar UserId/RecipeId relationships and invariants; central Domain must not reference this project.

User ownership: reference Users Domain.Contracts for UserId and shared user values. Keep foreign keys scalar; foreign aggregate CLR navigations are prohibited. PersistenceModel preserves the relational constraints with typed HasOne<T>() mappings.
