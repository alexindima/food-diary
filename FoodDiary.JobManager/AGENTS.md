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
- Register application modules only when a scheduled job or its application service requires them; keep the approved list enforced by `JobManagerGuardrailTests`.

## Dependencies
- Reuse Application abstractions/handlers instead of duplicating logic.
- Allowed production references are `FoodDiary.Application.Runtime`, explicitly scheduled application/module projects, `FoodDiary.Infrastructure`, and narrow shared transport adapters required by composition. Resource providers belong to their module Infrastructure projects.
- Keep the concrete feature-project list synchronized with `FoodDiary.JobManager.csproj` and `JobManagerGuardrailTests`; do not introduce an aggregate `FoodDiary.Application` dependency.
- Compose Notifications persistence and web-push provider through `AddNotificationsInfrastructure`; jobs and schedules remain host adapters.
- Compose localized notification text through `AddNotificationResources`; do not restore a central Resources dependency.
- Keep package versions aligned with central project conventions.

## Commands
- Build: `dotnet build FoodDiary.JobManager/FoodDiary.JobManager.csproj`
- Run: `dotnet run --project FoodDiary.JobManager`
- Tests: `dotnet test Hosts/tests/FoodDiary.JobManager.Tests/FoodDiary.JobManager.Tests.csproj`

Billing renewal and AI recognition workers dispatch Contracts requests through ISender. Keep loops, scopes, schedules, cancellation and job telemetry in the host; handlers own use-case orchestration. These workflow requests do not opt into automatic unit-of-work saves.

Dietologist client-task reminders dispatch SendClientTaskRemindersCommand through ISender. Its owner handler stages one batch and the common transactional command pipeline saves it. Keep scheduler options, retry/concurrency attributes and telemetry in the job.

Fasting notification scheduling and telemetry cleanup dispatch Contracts requests through ISender. The notification handler retains its explicit save and post-commit queue ordering; telemetry cleanup retains independently committed delete batches. Neither request acquires the automatic transactional-command marker.

WeeklyGoals reminders dispatch SendWeeklyGoalRemindersCommand through ISender. Preserve scheduling, retries, concurrency exclusion, options and telemetry. The handler retains per-batch saves; no automatic whole-job transaction.

UserCleanupJob dispatches one CleanupDeletedUsersCommand through ISender. Users Application owns paging; Users Infrastructure retains independent per-user transactions. Do not inject its internal cleanup port into the job.
