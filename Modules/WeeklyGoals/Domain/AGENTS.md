# Weekly Goals Domain Guidelines

## Scope

Rules for `Modules/WeeklyGoals/Domain/`.

## Role

- Own `WeeklyGoal`, `WeeklyGoalId`, `WeeklyGoalType`, and their invariants.
- Keep domain behavior independent from application, persistence, transport, and host concerns.
- Use `WeeklyGoalReminderSettings` for reminder mutations. It keeps local `TimeOnly` and UTC `TimeSpan` offset distinct, normalizes local time to whole minutes, and clears both values when disabled. Keep scalar minute fields for persistence/read projections and primitive aggregate methods as compatibility adapters; reminder dispatch and persisted-state defensive checks retain their behavior.

## Boundaries

- Reference only Users Domain.Contracts for shared UserId.
- Do not reference Application, Contracts, Infrastructure, EF Core, or ASP.NET packages.
- Keep database mapping, repositories, the shared `FoodDiaryDbContext`, migrations, and the model snapshot outside this project.

User ownership: reference Users Domain.Contracts for UserId and shared user values. Keep foreign keys scalar; foreign aggregate CLR navigations are prohibited. PersistenceModel preserves the relational constraints with typed HasOne<T>() mappings.

All module projects and tests use `FoodDiary.Modules.WeeklyGoals.<Project>` identities and namespaces matching physical folders. Projects are siblings, including Application.Abstractions and PersistenceModel. Namespace changes preserve database schema, historical migration metadata, HTTP payloads and runtime behavior.
