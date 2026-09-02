# Cycles Domain Guidelines

- Own Cycles aggregates, entities, enums, and strongly typed IDs while preserving legacy CLR namespaces and EF identity.
- Reference Users Domain for User, Users Domain.Contracts for UserId and shared Primitives for generic guards.
- Do not reference Application, Infrastructure, EF Core, or transport.
- Own BleedingType, CycleSymptomCategory, and OvulationTestResult in Enums with their existing FoodDiary.Domain.Enums namespaces, names, and numeric values. Consumer projects reference this owner directly; central Domain must not reference Cycles Domain.

User ownership: use Users Domain for aggregate navigations, Users Domain.Contracts for ID-only dependencies, and residual central Domain only for shared values. Preserve all existing relationships.

Generic DomainGuard belongs to FoodDiary.Domain.Primitives, referenced directly. Central Domain grants no friend access. This domain has no central Domain dependency.
