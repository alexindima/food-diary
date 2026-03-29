# FoodDiary.JobManager

Long-running Worker Service using Hangfire for recurring maintenance jobs.

## Jobs

| Job ID | Class | Default Schedule | Purpose |
|--------|-------|-----------------|---------|
| `image-assets-cleanup` | `ImageCleanupJob` | Hourly (`0 * * * *`) | Deletes orphaned image assets older than N hours (default 12) |
| `users-cleanup` | `UserCleanupJob` | Daily 3 AM (`0 3 * * *`) | Purges soft-deleted users past retention period (default 30 days) |

Both jobs use a **batch loop pattern**: repeatedly call cleanup service with `batchSize` until a batch returns fewer items than the batch size.

## Configuration

### ImageCleanupOptions (section: `ImageCleanup`)
- `OlderThanHours` (default 12), `BatchSize` (default 50), `Cron`

### UserCleanupOptions (section: `UserCleanup`)
- `RetentionDays` (default 30), `BatchSize` (default 50), `Cron`
- `ReassignUserId` (optional GUID — reassign deleted user content to this user)

All validated on startup.

## Architecture

- Bootstraps full Application + Infrastructure layers via `AddApplication()` / `AddInfrastructure()`
- Hangfire with PostgreSQL storage (same `DefaultConnection` as the main app)
- `RecurringJobsHostedService` registers jobs on `StartAsync`
- Job classes depend on Application-layer interfaces (`IImageAssetCleanupService`, `IUserCleanupService`)
- Telemetry: `fooddiary.job.execution.events`, `fooddiary.job.deleted_items`, `fooddiary.job.execution.duration`

## Dependencies

- Project references: `FoodDiary.Application`, `FoodDiary.Infrastructure`
- NuGet: `Hangfire.AspNetCore`, `Hangfire.Core`, `Hangfire.PostgreSql`, `Microsoft.Extensions.Hosting`, `Newtonsoft.Json`
