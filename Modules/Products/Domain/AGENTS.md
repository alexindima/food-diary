# Products domain

Own Product, product value objects, FoodQualityScore and FoodQualityGrade with stable CLR namespaces. Products Domain owns the existing common food-quality calculation used by other modules; its consumers may reference this assembly for scoring without receiving or mutating Product aggregates.

Keep dependencies one-way toward Products Domain.Contracts, Images Contracts, USDA Domain, Users Domain and shared Primitives. Never add Meals or Recipes dependencies or inverse foreign aggregate collections. Preserve all calculation bodies, validation order and aggregate invariants. MeasurementUnit belongs to Products Domain.Contracts. Comment and image URL limits are owner-local at 2048.
