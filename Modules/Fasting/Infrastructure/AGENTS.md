# Fasting Infrastructure Guidelines

## Scope

Rules for `Modules/Fasting/Infrastructure/` except the scoped `Model/` guide.

## Role

- Own Fasting repository implementations and module composition registration.
- Reuse the shared `FoodDiaryDbContext` while the application has one database and migration host.

## Boundaries

- Depend on Fasting Application/Domain and the shared Infrastructure project only in this outward adapter layer.
- The shared Infrastructure project must never reference this project; that would create a cycle.
- Register the complete runtime slice through `AddFastingModule`.
- Keep migrations and the shared model snapshot in `FoodDiary.Infrastructure`.
