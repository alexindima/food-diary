# Weekly Goals Domain Guidelines

## Scope

Rules for `Modules/WeeklyGoals/Domain/`.

## Role

- Own `WeeklyGoal`, `WeeklyGoalId`, `WeeklyGoalType`, and their invariants.
- Keep domain behavior independent from application, persistence, transport, and host concerns.
- Preserve existing `FoodDiary.Domain.*` CLR namespaces so EF model identity remains stable.

## Boundaries

- Reference only Users Domain.Contracts for shared UserId.
- Do not reference Application, Contracts, Infrastructure, EF Core, or ASP.NET packages.
- Keep database mapping, repositories, the shared `FoodDiaryDbContext`, migrations, and the model snapshot outside this project.

User ownership: use Users Domain for aggregate navigations, Users Domain.Contracts for ID-only dependencies, and the exact module or Primitives owner for shared values/guards. Preserve all existing relationships.
