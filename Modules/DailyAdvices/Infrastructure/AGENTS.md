# Daily Advices Infrastructure Guidelines

- Own the repository and complete module registration facade.
- Reuse the shared `FoodDiaryDbContext`; central Infrastructure must not reference this outer adapter.
- Keep migrations and the shared model snapshot central.
