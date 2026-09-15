# RecipeCommunity PersistenceModel

Own EF configurations and explicit ApplyRecipeCommunityPersistenceModel. Preserve all conversions, indexes, scalar User/Recipe foreign keys and cascade rules; shared DbContext/migrations stay central.

Reference Users.Domain.Contracts and Recipes.Domain.Contracts for scalar IDs. The three foreign Cascade relationships are owned by central RecipeCommunityCrossModuleRelationships. RecipeLike deliberately has no Recipe FK; preserve this and its unique user/recipe index.

Current module convention: all projects use `FoodDiary.Modules.RecipeCommunity.<Project>` assembly identities and namespaces matching their folders, including tests. Preserve historical migration metadata and database/HTTP contracts during namespace moves.
