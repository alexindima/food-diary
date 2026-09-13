# Recipes infrastructure

Own repositories, read projections, lookup/access adapters, EfRecipeMutationTransactionRunner and complete DI. Preserve scoped aliases and all SQL/access/tracking/transaction behavior. Reference central Infrastructure one-way for shared context creation, transaction coordination and shared transaction-attempt reset via explicit friend access; central Infrastructure references only Model, never this project. Exclude Model sources. No new public implementation visibility. Populate immutable ingredient product snapshots with a no-tracking batch projection; never track foreign Product aggregates while loading a recipe.

ADR 0038: reviewed cross-module SQL read implementations now live in FoodDiary.ReadModel.Composition, registered explicitly by hosts. Module writes and existing repository aliases stay here; modules never reference the composition assembly. Shared DbContext capabilities remain inventoried. See docs/adr/0038-read-model-composition.md.

RecipeRepository delegates usage counts to the owner IRecipeUsageQuery port. Host read composition owns the Meals/nested recipe SQL projection and uses the caller shared context and transaction.

RecipeRepository uses RecipesDbContext for all owned tracking and mutations. Registration shares the central connection and joins the live shared transaction before owner operations, including after intermediate UOW saves. Preserve aliases, split graph queries, xmin row locks and product snapshot hydration. The Serializable runner and user-purge participant retain their reviewed central coordination bridges.
