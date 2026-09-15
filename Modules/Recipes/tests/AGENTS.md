# Recipes tests

Module application tests own existing Recipes use cases and calculation behavior. Use canonical test namespaces; every test/helper has ExcludeFromCodeCoverage. Mixed central PostgreSQL fixture, Product advisory-lock coordination, Favorites, HTTP and DI suites remain with current owners. Recipe-to-Meal domain snapshot compatibility belongs to Meals.Domain.Tests. No test duplication or cross-test-project references. Run ordinary tests without coverage collectors and save VSTest TRX/counts.

Current module convention: all projects use `FoodDiary.Modules.Recipes.<Project>` assembly identities and namespaces matching their folders, including tests. Preserve historical migration metadata and database/HTTP contracts during namespace moves.
