# Fasting Domain Guidelines

## Scope

Rules for `Modules/Fasting/Domain/`.

## Role

- Own Fasting aggregates, entities, enums, and strongly typed identifiers.
- Keep domain behavior independent from application, persistence, transport, and host concerns.
- Preserve existing CLR namespaces during the extraction tranche so EF model identity and serialized enum contracts remain stable.

## Boundaries

- Reference Users Domain for User, Users Domain.Contracts for UserId and use shared Primitives for base types; validation helpers remain Fasting-owned.
- Do not reference Application, Contracts, Infrastructure, EF Core, or ASP.NET packages.
- Keep database mapping and repository behavior outside this project.

User ownership: use Users Domain for aggregate navigations, Users Domain.Contracts for ID-only dependencies, and the exact module or Primitives owner for shared values/guards. Preserve all existing relationships.
