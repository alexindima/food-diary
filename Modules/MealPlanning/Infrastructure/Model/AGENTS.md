# MealPlanning persistence model

Own all six MealPlans/ShoppingLists EF configurations and explicit model-builder
registration. No dependency on shared DbContext. Preserve legacy configuration
namespaces, tables, indexes, xmin, converters, field access, delete behavior and
User.ShoppingLists inverse navigation. Source provenance IDs are scalar indexed
values, not foreign keys. No new migration is expected from physical extraction.
