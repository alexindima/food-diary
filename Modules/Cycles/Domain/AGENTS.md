# Cycles Domain Guidelines

- Own Cycles aggregates, entities, enums, and strongly typed IDs while preserving legacy CLR namespaces and EF identity.
- Reference Users Domain for User, Users Domain.Contracts for UserId and central Domain for shared guards.
- Do not reference Application, Infrastructure, EF Core, or transport.
- Own BleedingType, CycleSymptomCategory, and OvulationTestResult in Enums with their existing FoodDiary.Domain.Enums namespaces, names, and numeric values. Consumer projects reference this owner directly; central Domain must not reference Cycles Domain.

User ownership: use Users Domain for aggregate navigations, Users Domain.Contracts for ID-only dependencies, and residual central Domain only for shared values/guards. Preserve all existing relationships.
