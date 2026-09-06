# Recipes persistence model

Own all three recipe EF configurations and ApplyRecipesPersistenceModel. Preserve CLR entity identity, mappings, xmin, indexes, conversions, field access, nullable relationships and delete behavior. Product and User relationships use scalar keys without foreign CLR navigations; ignore the transient ProductSnapshot. Central DbContext/migrations/snapshot stay central. Verify with actual PostgreSQL tests and EF has-pending-model-changes.
