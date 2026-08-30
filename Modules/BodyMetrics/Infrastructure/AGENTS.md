# BodyMetrics Infrastructure Guidelines

## Scope

Rules for `Modules/BodyMetrics/Infrastructure/`.

## Boundaries

- Own BodyMetrics repository implementations and complete module registration.
- Preserve all user predicates, ordering, tracking, date, and cancellation behavior.
- Keep central `FoodDiaryDbContext`, migrations, and snapshot outside this module.
