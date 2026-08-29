# Fasting Domain Guidelines

## Scope

Rules for `Modules/Fasting/Domain/`.

## Role

- Own Fasting aggregates, entities, enums, and strongly typed identifiers.
- Keep domain behavior independent from application, persistence, transport, and host concerns.
- Preserve existing CLR namespaces during the extraction tranche so EF model identity and serialized enum contracts remain stable.

## Boundaries

- Reference only the shared/core domain project while shared `User` and `UserId` ownership remains centralized.
- Do not reference Application, Contracts, Infrastructure, EF Core, or ASP.NET packages.
- Keep database mapping and repository behavior outside this project.
