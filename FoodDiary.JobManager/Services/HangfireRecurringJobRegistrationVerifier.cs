using Hangfire;
using Hangfire.Storage;

namespace FoodDiary.JobManager.Services;

public sealed class HangfireRecurringJobRegistrationVerifier(
    JobStorage jobStorage,
    ILogger<HangfireRecurringJobRegistrationVerifier> logger) : IRecurringJobRegistrationVerifier {
    public void EnsureRegistered(IReadOnlyCollection<string> expectedJobIds) {
        ArgumentNullException.ThrowIfNull(expectedJobIds);

        using var connection = jobStorage.GetConnection();
        var registeredJobIds = connection
            .GetRecurringJobs()
            .Select(job => job.Id)
            .Where(static id => !string.IsNullOrWhiteSpace(id))
            .ToHashSet(StringComparer.Ordinal);

        var missingJobIds = expectedJobIds
            .Where(expectedJobId => !registeredJobIds.Contains(expectedJobId))
            .ToArray();

        if (missingJobIds.Length == 0) {
            logger.LogInformation(
                "Verified Hangfire recurring job registration for {JobCount} jobs.",
                expectedJobIds.Count);
            return;
        }

        throw new InvalidOperationException(
            $"Recurring Hangfire jobs were not registered: {string.Join(", ", missingJobIds)}");
    }
}
