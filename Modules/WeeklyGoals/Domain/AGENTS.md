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

User ownership: reference Users Domain.Contracts for UserId and shared user values. Keep foreign keys scalar; foreign aggregate CLR navigations are prohibited. PersistenceModel preserves the relational constraints with typed HasOne<T>() mappings.
