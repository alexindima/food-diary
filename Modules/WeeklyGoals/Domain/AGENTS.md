# Weekly Goals Domain Guidelines

## Scope

Rules for `Modules/WeeklyGoals/Domain/`.

## Role

- Own `WeeklyGoal`, `WeeklyGoalId`, `WeeklyGoalType`, and their invariants.
- Keep domain behavior independent from application, persistence, transport, and host concerns.
- Preserve existing `FoodDiary.Domain.*` CLR namespaces so EF model identity remains stable.

## Boundaries

- Reference only the central Domain project while shared `UserId` ownership remains centralized.
- Do not reference Application, Contracts, Infrastructure, EF Core, or ASP.NET packages.
- Keep database mapping, repositories, the shared `FoodDiaryDbContext`, migrations, and the model snapshot outside this project.
