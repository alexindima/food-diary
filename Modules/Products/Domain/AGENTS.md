# Products domain

Own Product and exclusively Product-owned value objects with stable CLR namespaces.
Keep dependencies one-way toward central shared domain types, Products Domain.Contracts,
Images Contracts, USDA Domain and the narrow shared Nutrition Domain for scoring and units. Do not add Meals or Recipes dependencies or inverse
foreign aggregate collections. Preserve aggregate invariants and scalar meal snapshots.

Generic DomainGuard belongs to FoodDiary.Domain.Primitives, referenced directly. Central Domain grants no friend access.
