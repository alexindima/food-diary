# Meals Persistence Model

Own Meal EF configurations, the MealRecognitionReceipt mapping and explicit model registration. Preserve
all CLR/EF names, indexes, defaults, relationships and delete behavior. The
shared context applies this model explicitly. UserConfiguration, historical
migrations and snapshot remain central.

Recognition receipts deliberately have no Meal foreign key: they retain replay
protection after meal deletion. The required User cascade FK removes receipts on
account purge. OperationId is permanent and (UserId, RecognitionId) is unique.

ADR 0032 intentionally changes ImageAsset foreign keys to ClientNoAction (database NO ACTION): preserve saved image references during concurrent cleanup. Keep the matching shared migration and snapshot; other delete policies remain as declared.

Recipes and Meals extend scalar persistence protection to twenty-five models. Their ten foreign FKs live in RecipesCrossModuleRelationships and MealsCrossModuleRelationships; all optionality and delete policies remain unchanged, including four image ClientNoAction mappings. Owned nested Recipe Restrict and Meal/Ai cascades stay local. Recognition receipts retain their User Cascade FK and deliberately have no Meal FK. The models consume direct ID contracts; existing central Domain references suffice. No module PersistenceModel retains a foreign Domain project reference.
