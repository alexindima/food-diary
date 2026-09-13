# Daily Advices Infrastructure Guidelines

- Own the repository and complete module registration facade.
- Own the single-entity `DailyAdvicesDbContext`; registration obtains the shared connection through `FoodDiaryDbContext.CreateModuleContext`. The repository receives only its owned DbSet and remains no-tracking/read-only. Central Infrastructure must not reference this outer adapter.
- Keep migrations and the shared model snapshot central.
