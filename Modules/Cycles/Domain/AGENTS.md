# Cycles Domain Guidelines

- Own Cycles aggregates, entities, enums, and strongly typed IDs while preserving legacy CLR namespaces and EF identity.
- Reference Users Domain.Contracts for UserId and shared Primitives for generic guards.
- Do not reference Application, Infrastructure, EF Core, or transport.
- Own BleedingType, CycleSymptomCategory, and OvulationTestResult in Enums with their existing FoodDiary.Domain.Enums namespaces, names, and numeric values. Consumer projects reference this owner directly; central Domain must not reference Cycles Domain.

User ownership: reference Users Domain.Contracts for UserId and shared user values. Keep foreign keys scalar; foreign aggregate CLR navigations are prohibited. PersistenceModel preserves the relational constraints with typed HasOne<T>() mappings.

Generic DomainGuard belongs to FoodDiary.Domain.Primitives, referenced directly. Central Domain grants no friend access. This domain has no central Domain dependency.

CycleId remains a public strongly typed Guid identifier in ValueObjects/Ids. Preserve its legacy namespace, conversions, Empty/New and ToString semantics even though it currently has no production consumer.
