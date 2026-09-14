# Fasting Infrastructure Guidelines

## Scope

Rules for `Modules/Fasting/Infrastructure/` except the scoped `Model/` guide.

## Role

- Own Fasting repository implementations and module composition registration.
- Own `FastingDbContext` for runtime tracking of the five Fasting entities. Repositories receive narrow owned DbSets; registration uses the central context factory and shared unit of work.
- Keep standalone telemetry bulk cleanup independent of tracked SaveChanges; preserve batching and cancellation.

## Boundaries

- Depend on Fasting Application/Domain and the narrow FoodDiary.Persistence.Abstractions factory contract. Do not reference central Infrastructure directly or transitively.
- The shared Infrastructure project must never reference this project; that would create a cycle.
- Register the complete runtime slice through `AddFastingModule`.
- Keep migrations and the shared model snapshot in `FoodDiary.Infrastructure`.

Read reminder settings through Users.Contracts IUserFastingReminderReadService in
one batch of distinct active-occurrence user IDs. Do not read Users sets or depend
on Users.Domain. Preserve occurrence order, Plan loading and missing-user omission.
