# Cycles Domain Guidelines

- Own Cycles aggregates, entities, enums, and strongly typed IDs while preserving legacy CLR namespaces and EF identity.
- Reference central Domain only for shared `User`, `UserId`, and domain primitives.
- Do not reference Application, Infrastructure, EF Core, or transport.
