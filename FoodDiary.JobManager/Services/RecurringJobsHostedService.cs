using Hangfire;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace FoodDiary.JobManager.Services;

public sealed class RecurringJobsHostedService(
    IRecurringJobManager recurringJobManager,
    IOptions<ImageCleanupOptions> options,
    IOptions<UserCleanupOptions> userCleanupOptions,
    ImageCleanupJob imageCleanupJob,
    UserCleanupJob userCleanupJob) : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken)
    {
        var settings = options.Value;
        var userSettings = userCleanupOptions.Value;
        recurringJobManager.AddOrUpdate(
            "image-assets-cleanup",
            () => imageCleanupJob.Execute(CancellationToken.None),
            settings.Cron);
        recurringJobManager.AddOrUpdate(
            "users-cleanup",
            () => userCleanupJob.Execute(CancellationToken.None),
            userSettings.Cron);

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}

