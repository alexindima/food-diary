# Products domain contracts

Own ProductId, ProductType and MeasurementUnit with canonical folder namespaces and unchanged numeric values. Reference only shared Domain.Primitives. Product aggregates belong to Products Domain; food-quality algorithms belong to Products FoodQuality. Never reference either implementation from this contract seam.

Current module convention: all projects use `FoodDiary.Modules.Products.<Project>` assembly identities and namespaces matching their folders, including tests. Preserve historical migration metadata and database/HTTP contracts during namespace moves.
