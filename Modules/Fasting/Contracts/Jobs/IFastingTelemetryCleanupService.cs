namespace FoodDiary.Modules.Fasting.Contracts.Jobs;

public interface IFastingTelemetryCleanupService {
    Task<int> CleanupAsync(
        DateTime olderThanUtc,
        int batchSize,
        CancellationToken cancellationToken);
}
