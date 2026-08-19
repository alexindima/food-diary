namespace FoodDiary.Application.Fasting.Services;

public interface IFastingTelemetryCleanupService {
    Task<int> CleanupAsync(
        DateTime olderThanUtc,
        int batchSize,
        CancellationToken cancellationToken);
}
