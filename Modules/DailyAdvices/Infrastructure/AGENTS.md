# Daily Advices Infrastructure Guidelines

- Own the repository and complete module registration facade.
- Own the single-entity `DailyAdvicesDbContext`; registration obtains the shared connection through `IModuleContextFactory.CreateModuleContext`. Repositories receive only their owned DbSet. DailyAdviceRepository remains no-tracking/read-only; DailyAdviceWriteRepository stages imports without saving, and the Admin transactional command owns the coordinated save. Central Infrastructure must not reference this outer adapter.
- Keep migrations and the shared model snapshot central.
