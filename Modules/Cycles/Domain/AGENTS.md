# Cycles Domain Guidelines

- Own Cycles aggregates, entities, and strongly typed IDs with canonical namespaces and unchanged relational mappings.
- Reference Users Domain.Contracts for UserId and shared Primitives for generic guards.
- Do not reference Application, Infrastructure, EF Core, or transport.
- Consume cycle enums through Cycles Domain.Contracts. Preserve names and numeric values; aggregate and ID ownership remains here.

User ownership: reference Users Domain.Contracts for UserId and shared user values. Keep foreign keys scalar; foreign aggregate CLR navigations are prohibited. PersistenceModel preserves the relational constraints with typed HasOne<T>() mappings.

Generic DomainGuard belongs to FoodDiary.Domain.Primitives, referenced directly. Central Domain grants no friend access. This domain has no central Domain dependency.
