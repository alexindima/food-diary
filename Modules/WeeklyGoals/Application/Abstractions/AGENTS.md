# Weekly Goals Application Abstractions Guidelines

Repository ports and transaction seams live here. Depend on the WeeklyGoals Domain project for module-owned types; reference Users Domain.Contracts directly for `UserId`. Preserve legacy CLR namespaces and do not reference EF Core, hosts, presentation, central Infrastructure, or application implementations.
