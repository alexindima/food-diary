# Hydration Application Abstractions Guidelines

Repository ports and internal persistence projections live here. Depend on Hydration Domain, Users Domain.Contracts and shared result primitives. Do not reference EF Core, hosts, presentation, or Infrastructure.

Own HydrationEntryErrors, preserving not-found privacy classification and timestamp formatting. Callers use this factory directly; the central Errors.HydrationEntry facade is retired.
