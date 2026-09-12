# Meals Infrastructure

Own MealRepository, persistence registration and complete Meals DI. Depend on
central Infrastructure for the shared context. Central Infrastructure must not
reference this adapter project. Preserve query ordering, access predicates,
tracking behavior and cancellation.

EfMealRecognitionTransactionRunner owns a short user-serialized PostgreSQL transaction
for creation receipts and undo. Its intermediate unit-of-work flush captures the new
Meal xmin; the final receipt save and commit remain in the same transaction. Use the
shared attempt reset and keep provider calls outside the transaction. Undo locks the
owned Meal row before comparing its version. See ADR 0037.

MealItemDisplayReadService owns the existing Dashboard snapshot/fallback and food-quality policy. Keep a single no-tracking batch query, owner filtering and stable ordering. Distinct meal-detail legacy recipe fallbacks retain their existing semantics.
