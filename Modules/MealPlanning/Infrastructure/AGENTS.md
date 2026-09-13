# MealPlanning persistence adapters

Own MealPlans and ShoppingLists repositories and complete AddMealPlanningModule
registration. Preserve existing repository CLR namespaces for in-repository
consumers. Use the owned runtime sets for writes; do not save or begin a transaction inside
these repositories. Keep user filters and tracking/projection behavior unchanged.
Central Infrastructure references Model, never this adapter project.

Runtime writes use the owned MealPlanningDbContext created by the central factory and
shared unit of work (ADR 0040). Central migration mappings remain.
MealPlanRepository receives only its owned DbSet and IMealPlanCompositionReader.
Cross-module SQL joins and immutable recipe snapshots are implemented by the host
ReadModel.Composition adapter. Keep aggregate loading and snapshot attachment in
this owner. Never reference the composition implementation from this assembly.
ShoppingList repositories receive only their owned set; user purge continues on
the central transaction participant.
