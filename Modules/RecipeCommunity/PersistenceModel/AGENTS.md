# RecipeCommunity PersistenceModel

Own EF configurations and explicit ApplyRecipeCommunityPersistenceModel. Preserve all conversions, indexes, scalar User/Recipe foreign keys and cascade rules; shared DbContext/migrations stay central.

Reference Users.Domain.Contracts and Recipes.Domain.Contracts for scalar IDs. The four foreign Cascade relationships are owned by central RecipeCommunityCrossModuleRelationships. RecipeLike requires an existing Recipe and preserves its unique user/recipe index. The central migration removes only legacy likes whose recipe is already missing before adding the Recipe foreign key; deleting a recipe cascades its likes. This foreign key also makes the owner's cleanup row lock protect concurrent like insertion.

Current module convention: all projects use `FoodDiary.Modules.RecipeCommunity.<Project>` assembly identities and namespaces matching their folders, including tests. Preserve historical migration metadata and database/HTTP contracts during namespace moves.
