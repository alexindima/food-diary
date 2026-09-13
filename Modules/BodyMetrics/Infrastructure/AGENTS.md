# BodyMetrics Infrastructure Guidelines

## Scope

Rules for `Modules/BodyMetrics/Infrastructure/`.

## Boundaries

- Own BodyMetrics repository implementations and complete module registration.
- Preserve all user predicates, ordering, tracking, date, and cancellation behavior.
- Keep central `FoodDiaryDbContext`, migrations, and snapshot outside this module.
- BodyMetricsDbContext owns runtime WeightEntry/WaistEntry tracking. Registration uses the central CreateModuleContext factory and supplies only owned DbSets to repositories. Save through the shared IUnitOfWork; never commit in repositories. Retain the central migration/read/purge model and Users-owned goals. See ADR 0040.
