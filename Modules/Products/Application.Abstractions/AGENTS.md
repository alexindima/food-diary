# Products Application.Abstractions

Owner-only aggregate repositories and mutation transaction port. ProductErrors belongs to Products.Contracts. Depend on Products Domain, Users Domain.Contracts and Results; preserve CLR namespaces. Never expose aggregate repositories to foreign modules.

Current module convention: all projects use `FoodDiary.Modules.Products.<Project>` assembly identities and namespaces matching their folders, including tests. Preserve historical migration metadata and database/HTTP contracts during namespace moves.
