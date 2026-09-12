# Recipes infrastructure

Own repositories, read projections, lookup/access adapters, EfRecipeMutationTransactionRunner and complete DI. Preserve scoped aliases and all SQL/access/tracking/transaction behavior. Reference central Infrastructure one-way for DbContext and shared transaction-attempt reset via explicit friend access; central Infrastructure references only Model, never this project. Exclude Model sources. No new public implementation visibility. Populate immutable ingredient product snapshots with a no-tracking batch projection; never track foreign Product aggregates while loading a recipe.

ADR 0038: reviewed cross-module SQL read implementations now live in FoodDiary.ReadModel.Composition, registered explicitly by hosts. Module writes and existing repository aliases stay here; modules never reference the composition assembly. Shared DbContext capabilities remain inventoried. See docs/adr/0038-read-model-composition.md.
