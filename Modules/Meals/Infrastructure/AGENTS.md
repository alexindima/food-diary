# Meals Infrastructure

Own MealRepository, persistence registration and complete Meals DI. Depend on
central Infrastructure for the shared context. Central Infrastructure must not
reference this adapter project. Preserve query ordering, access predicates,
tracking behavior and cancellation.

MealItemDisplayReadService owns the existing Dashboard snapshot/fallback and food-quality policy. Keep a single no-tracking batch query, owner filtering and stable ordering. Distinct meal-detail legacy recipe fallbacks retain their existing semantics.
