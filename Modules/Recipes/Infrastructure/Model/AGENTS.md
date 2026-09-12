# Recipes persistence model

Own all three recipe EF configurations and ApplyRecipesPersistenceModel. Preserve CLR entity identity, mappings, xmin, indexes, conversions, field access, nullable relationships and delete behavior. Product and User relationships use scalar keys without foreign CLR navigations; ignore the transient ProductSnapshot. Central DbContext/migrations/snapshot stay central. Verify with actual PostgreSQL tests and EF has-pending-model-changes.

ADR 0032 intentionally changes ImageAsset foreign keys to ClientNoAction (database NO ACTION): preserve saved image references during concurrent cleanup. Keep the matching shared migration and snapshot; other delete policies remain as declared.

Recipes and Meals extend scalar persistence protection to twenty-five models. Their ten foreign FKs live in RecipesCrossModuleRelationships and MealsCrossModuleRelationships; all optionality and delete policies remain unchanged, including four image ClientNoAction mappings. Owned nested Recipe Restrict and Meal/Ai cascades stay local. Recognition receipts retain their User Cascade FK and deliberately have no Meal FK. The models consume direct ID contracts; existing central Domain references suffice. No module PersistenceModel retains a foreign Domain project reference.
