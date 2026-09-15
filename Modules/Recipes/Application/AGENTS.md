# Recipes application

Own Commands, Queries, mappings, recent/explore orchestration, nutrition and mutation workflows. Use canonical assembly and folder namespaces; project identity is FoodDiary.Modules.Recipes.Application. Each request slice has its own folder. Application.Abstractions is a sibling project. AddRecipesApplication registers handlers/validators; hosts use complete infrastructure AddRecipesModule.

Consume Products, Users, Images, Favorites and RecentItems through existing semantic capabilities. Preserve transactions, cycle detection, manual nutrition, rounding and media lifecycle. Run module application tests plus current consumer, HTTP and architecture suites.

Current module convention: all projects use `FoodDiary.Modules.Recipes.<Project>` assembly identities and namespaces matching their folders, including tests. Preserve historical migration metadata and database/HTTP contracts during namespace moves.
