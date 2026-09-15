# Hydration Application Abstractions Guidelines

Repository ports and internal persistence projections live here. Depend on Hydration Domain, Users Domain.Contracts and shared result primitives. Do not reference EF Core, hosts, presentation, or Infrastructure.

Own HydrationEntryErrors, preserving not-found privacy classification and timestamp formatting. Callers use this factory directly; the central Errors.HydrationEntry facade is retired.

Use canonical FoodDiary.Modules.Hydration project identities and folder namespaces, including tests. Projects are siblings. Preserve historical migration metadata and relational schema.
