# Meals Persistence Model

Own the four Meal EF configurations and explicit model registration. Preserve
all CLR/EF names, indexes, defaults, relationships and delete behavior. The
shared context applies this model explicitly. UserConfiguration, historical
migrations and snapshot remain central.

ADR 0032 intentionally changes ImageAsset foreign keys to ClientNoAction (database NO ACTION): preserve saved image references during concurrent cleanup. Keep the matching shared migration and snapshot; other delete policies remain as declared.
