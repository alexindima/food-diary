# RecipeCommunity Domain

Own RecipeComment, RecipeLike and their IDs. Preserve namespaces, User/Recipe navigations and invariants; central Domain must not reference this project.

User ownership: use Users Domain for aggregate navigations, Users Domain.Contracts for ID-only dependencies, and the exact module or Primitives owner for shared values/guards. Preserve all existing relationships.
