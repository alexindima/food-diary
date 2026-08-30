# Cycles Infrastructure Guidelines

- Own the Cycles repository and complete module registration facade.
- Reuse central `FoodDiaryDbContext`; central Infrastructure must not reference this outer adapter.
- Keep migrations and the shared model snapshot central.
