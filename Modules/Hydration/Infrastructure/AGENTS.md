# Hydration Infrastructure Guidelines

Hydration repository implementations and the complete `AddHydrationModule` composition facade live here. The facade obtains `FoodDiaryDbContext.HydrationEntries` from the scoped shared context and injects only `DbSet<HydrationEntry>` into the repository. Do not inject the full context into that adapter or introduce another context/transaction owner.

The caller retains SaveChanges, transaction and ChangeTracker ownership. Central Infrastructure must never reference this project. Migrations remain central. PostgreSQL tests cover rollback, no-tracking reads, user cascade and unchanged constraints; see ADR 0029.
