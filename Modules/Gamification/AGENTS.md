# Gamification Logical Module Guidelines

## Scope

Rules for `Modules/Gamification/`.

## Boundary

- Own achievement evaluation, awarding, reconciliation, administration, reads, achievement domain types, application ports, EF adapters, and owned EF configurations.
- Preserve the legacy `FoodDiary.Application.Gamification` assembly and existing CLR namespaces.
- Register the complete module through Infrastructure's `AddGamificationModule`; hosts remain composition roots.
- Keep `FoodDiaryDbContext`, historical migrations, snapshot, and the shared achievement outbox message/dead-letter integration central.
- Read Meals, Dashboard, Users, and Lessons only through application-level capabilities.

## Tests

- Keep module-owned Application, Domain, and Infrastructure unit tests under `Modules/Gamification/tests/`.
- Keep HTTP, host, shared DbContext/integration, architecture, and cross-module tests central.
