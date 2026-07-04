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
- Keep this project free of HTTP presentation concerns; do not reference `FoodDiary.Web.Api` or `FoodDiary.Presentation.Api`.
- Put scheduler/worker plumbing here and use application/infrastructure services for actual work.
- Keep recurring job registration auditable and covered by tests when schedules or options change.

## Dependencies
- Reuse Application abstractions/handlers instead of duplicating logic.
- Allowed production references are `FoodDiary.Application`, `FoodDiary.Infrastructure`, `FoodDiary.Integrations`, and `FoodDiary.Resources`.
- Keep package versions aligned with central project conventions.

## Commands
- Build: `dotnet build FoodDiary.JobManager/FoodDiary.JobManager.csproj`
- Run: `dotnet run --project FoodDiary.JobManager`
- Tests: `dotnet test tests/FoodDiary.JobManager.Tests/FoodDiary.JobManager.Tests.csproj`
