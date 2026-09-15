# Meals tests

Meals-only application and module-domain invariant tests live here without
duplicates. MealRepository PostgreSQL coverage uses linked central fixtures.
Mixed HTTP, reporting, DbContext, migration and cross-module suites remain with
their proven owners. Do not run coverage collectors.

Domain.Tests owns RecipeSnapshotCompatibilityTests from the retired mixed Domain
suite. Preserve its real Recipe-to-Meal snapshot and AI-source behavior; its direct
Recipes.Domain test reference must not become a production dependency.
