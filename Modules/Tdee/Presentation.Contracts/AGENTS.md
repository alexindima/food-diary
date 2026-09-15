# Reusable presentation contracts

Own immutable wire DTOs with preserved CLR names and JSON shapes. Reference only
other Presentation.Contracts required by composite responses. No mappings, methods,
application models, controllers, framework packages, providers or persistence.

Current module convention: all projects use `FoodDiary.Modules.Tdee.<Project>` assembly identities and namespaces matching their folders, including tests. Preserve historical migration metadata and database/HTTP contracts during namespace moves.
