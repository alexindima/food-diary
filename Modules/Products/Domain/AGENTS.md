# Products domain

Own Product and product value objects with canonical folder namespaces. Consume the Products-owned FoodQuality assembly for FoodQualityScore and FoodQualityGrade. Foreign scoring consumers reference that narrow assembly directly, not this aggregate-bearing Domain.

Keep dependencies one-way toward Products Domain.Contracts and FoodQuality, Images Contracts, Users Domain.Contracts and shared Primitives. Never add Meals or Recipes dependencies or inverse foreign aggregate collections. Preserve all calculation bodies, validation order and aggregate invariants. MeasurementUnit belongs to Products Domain.Contracts. Comment and image URL limits are owner-local at 2048.

Current module convention: all projects use `FoodDiary.Modules.Products.<Project>` assembly identities and namespaces matching their folders, including tests. Preserve historical migration metadata and database/HTTP contracts during namespace moves.
