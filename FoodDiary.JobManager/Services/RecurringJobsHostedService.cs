using Hangfire;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace FoodDiary.JobManager.Services;

public sealed class RecurringJobsHostedService(
    IRecurringJobManager recurringJobManager,
    IOptions<ImageCleanupOptions> options,
    ImageCleanupJob imageCleanupJob) : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken)
    {
        var settings = options.Value;
        recurringJobManager.AddOrUpdate(
            "image-assets-cleanup",
            () => imageCleanupJob.Execute(CancellationToken.None),
            settings.Cron);

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}

