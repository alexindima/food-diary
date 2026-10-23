# Recipes infrastructure

Own repositories, read projections, lookup/access adapters, EfRecipeMutationTransactionRunner and complete DI. Preserve scoped aliases and all SQL/access/tracking/transaction behavior. Use IModuleContextFactory for owner creation and IModuleTransactionCoordinator for Serializable mutation execution, retries/reset and live transaction access. The purge participant uses RecipesDbContext with live coordinator transaction binding; this project no longer references central Infrastructure. Central Infrastructure references only Model, never this project. PersistenceModel is a sibling project. No new public implementation visibility. Populate immutable ingredient product snapshots with a no-tracking batch projection; never track foreign Product aggregates while loading a recipe.

ADR 0038: reviewed cross-module SQL read implementations now live in FoodDiary.ReadModel.Composition, registered explicitly by hosts. Module writes and existing repository aliases stay here; modules never reference the composition assembly. Shared DbContext capabilities remain inventoried. See docs/adr/0038-read-model-composition.md.

RecipeRepository delegates usage counts to the owner IRecipeUsageQuery port. Host read composition owns the Meals/nested recipe SQL projection and uses the caller shared context and transaction.

RecipeRepository uses RecipesDbContext for all owned tracking and mutations. Registration shares the central connection and joins the live shared transaction before owner operations, including after intermediate UOW saves. Preserve aliases, split graph queries, xmin row locks and product snapshot hydration. The Serializable runner delegates to the host coordinator; the user-purge participant uses its owner context.

RecipesUserContentRetentionParticipant retains only the source user's reader-selected
recipe closure before ordinary account purge. The composed reader owns surviving-reference
selection; the owner adapter transfers legacy covers and all galleries through the
source-filtered Images capability inside the existing per-user transaction. It never saves or commits.

Its lock phase selects all source-owned Recipe IDs in stable order FOR UPDATE
before any authoritative retention read. Preserve these locks until the caller
commits or rolls back, so incoming consumer FK inserts cannot succeed unnoticed
between reference selection and deletion. Product locks follow Recipe locks.

Current module convention: all projects use `FoodDiary.Modules.Recipes.<Project>` assembly identities and namespaces matching their folders, including tests. Preserve historical migration metadata and database/HTTP contracts during namespace moves.
