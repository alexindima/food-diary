# Fasting Persistence Model Guidelines

## Scope

Rules for `Modules/Fasting/Infrastructure/Model/`.

## Role

- Own EF Core configurations for Fasting entities.
- Expose one model-builder registration seam consumed by the shared migration host.
- Preserve table names, columns, indexes, relationships, and conversions during physical extraction.

## Boundaries

- Depend on Fasting Domain, Users.Domain.Contracts and EF Core only.
- Do not reference `FoodDiary.Infrastructure` or application projects; this keeps the project graph acyclic.
- Keep migrations and the shared model snapshot in the central migration host.

FastingCrossModuleRelationships in central Infrastructure configures the four UserId
Cascade foreign keys after owned model registrations. Keep local entity mappings
and same-owner relationships here; do not restore a Users.Domain dependency.
