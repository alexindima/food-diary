# Recipes ports

Own aggregate repository interfaces, owner-local nutrition writer, mutation transaction runner and RecipeErrors. Preserve public CLR signatures. These are persistence capabilities, not permission for other modules to mutate Recipe aggregates. Depend only on central Domain and shared Results; no central application abstractions reference (that would cycle).
