# Meals Infrastructure

Own MealRepository, persistence registration and complete Meals DI. Use IModuleTransactionCoordinator for coordinated transactions and live purge enlistment;
this adapter no longer references central Infrastructure. MealsDbContext owns the runtime meal graph and recognition receipts. Central Infrastructure must not
reference this adapter project. Preserve query ordering, access predicates,
tracking behavior and cancellation.

EfMealRecognitionTransactionRunner owns a short user-serialized PostgreSQL transaction
for creation receipts and undo. Its intermediate unit-of-work flush captures the new
Meal xmin; the final receipt save and commit remain in the same transaction. Use the
shared attempt reset and keep provider calls outside the transaction. Undo locks the
owned Meal row before comparing its version. See ADR 0037.

MealItemDisplayReadService owns the existing Dashboard snapshot/fallback and food-quality policy. Keep a single no-tracking batch query, owner filtering and stable ordering. Distinct meal-detail legacy recipe fallbacks retain their existing semantics.

ADR 0038: reviewed cross-module SQL read implementations now live in FoodDiary.ReadModel.Composition, registered explicitly by hosts. Module writes and existing repository aliases stay here; modules never reference the composition assembly. Shared DbContext capabilities remain inventoried. See docs/adr/0038-read-model-composition.md.

MealRepository obtains AI-session image URLs and legacy recipe fallback fields through IMealSourceSnapshotQuery. The host composition implementation returns immutable scalar snapshots and preserves existing source-ID lookup semantics. Keep snapshot precedence and the one-serving rule for snapshotted recipes; do not reintroduce foreign aggregate materialization. Meals owns graph loading and projection policy.

Meals repositories use the owner context or its narrow DbSet. The transaction runner delegates top-level execution and live transaction access to IModuleTransactionCoordinator and captures xmin from the owner tracker through the existing IUnitOfWork flush. Synchronize the current shared transaction before every owner query, especially after intermediate unit-of-work flushes. Purge retains order 50, deleting MealItems before Meals through MealsDbContext. Preserve AI graph cascades and recognition receipts until the final User FK cascade; Users owns the encompassing transaction. Migrations remain central.
