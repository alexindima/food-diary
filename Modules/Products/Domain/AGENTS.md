# Products domain

Own Product and product value objects with stable CLR namespaces. Consume the Products-owned FoodQuality assembly for FoodQualityScore and FoodQualityGrade. Foreign scoring consumers reference that narrow assembly directly, not this aggregate-bearing Domain.

Keep dependencies one-way toward Products Domain.Contracts, Images Contracts, USDA Domain, Users Domain and shared Primitives. Never add Meals or Recipes dependencies or inverse foreign aggregate collections. Preserve all calculation bodies, validation order and aggregate invariants. MeasurementUnit belongs to Products Domain.Contracts. Comment and image URL limits are owner-local at 2048.
