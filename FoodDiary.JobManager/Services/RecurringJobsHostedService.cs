using Hangfire;
using Microsoft.Extensions.Options;

namespace FoodDiary.JobManager.Services;

public sealed class RecurringJobsHostedService(
    IRecurringJobManager recurringJobManager,
    IRecurringJobRegistrationVerifier recurringJobRegistrationVerifier,
    IOptions<ImageCleanupOptions> options,
    IOptions<UserCleanupOptions> userCleanupOptions,
    ImageCleanupJob imageCleanupJob,
    UserCleanupJob userCleanupJob) : IHostedService {
    public Task StartAsync(CancellationToken cancellationToken) {
        var settings = options.Value;
        var userSettings = userCleanupOptions.Value;
        var imageCron = string.IsNullOrWhiteSpace(settings.Cron) ? "0 * * * *" : settings.Cron;
        var userCron = string.IsNullOrWhiteSpace(userSettings.Cron) ? "0 3 * * *" : userSettings.Cron;
        recurringJobManager.AddOrUpdate(
            RecurringJobIds.ImageAssetsCleanup,
            () => imageCleanupJob.Execute(CancellationToken.None),
            imageCron);
        recurringJobManager.AddOrUpdate(
            RecurringJobIds.UsersCleanup,
            () => userCleanupJob.Execute(CancellationToken.None),
            userCron);
        recurringJobRegistrationVerifier.EnsureRegistered(RecurringJobIds.All);

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
