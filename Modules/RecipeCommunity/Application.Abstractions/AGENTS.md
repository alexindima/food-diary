# RecipeCommunity Application.Abstractions

Own comment/like repository ports, projections and errors. Use canonical folder namespaces. Depend only on module Domain and Results; never reference central Application.Abstractions (its compatibility facade has been retired).

Current module convention: all projects use `FoodDiary.Modules.RecipeCommunity.<Project>` assembly identities and namespaces matching their folders, including tests. Preserve historical migration metadata and database/HTTP contracts during namespace moves.
