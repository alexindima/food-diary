# Products domain contracts

Own ProductId, ProductType and MeasurementUnit with canonical folder namespaces and unchanged numeric values. Reference only shared Domain.Primitives. Product aggregates belong to Products Domain; food-quality algorithms belong to Products FoodQuality. Never reference either implementation from this contract seam.

`ProductUnitQuantity` represents a consumed/ingredient amount in its owning product's declared units, including fractional values. It is not a gram conversion or a product base amount. Keep its positive finite 1,000,000 upper bound and scalar wire/storage mappings; never add implicit double conversions or an invalid zero struct default. Meals and Recipes mutations consume this exact owner value.

Current module convention: all projects use `FoodDiary.Modules.Products.<Project>` assembly identities and namespaces matching their folders, including tests. Preserve historical migration metadata and database/HTTP contracts during namespace moves.
