# Job Manager Guidelines

## Scope
Rules for `FoodDiary.JobManager/`.

## Responsibilities
- Background/scheduled jobs orchestration.
- Coordination with Application services and Infrastructure implementations.

## Rules
- Keep jobs idempotent where possible.
- Keep retry/error handling explicit and observable.
- Avoid embedding core business rules in scheduler plumbing.

## Dependencies
- Reuse Application abstractions/handlers instead of duplicating logic.
- Keep package versions aligned with central project conventions.

## Commands
- Build: `dotnet build FoodDiary.JobManager/FoodDiary.JobManager.csproj`
- Run: `dotnet run --project FoodDiary.JobManager`
