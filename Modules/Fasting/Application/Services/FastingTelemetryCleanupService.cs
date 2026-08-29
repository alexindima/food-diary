namespace FoodDiary.Modules.Fasting.Application.Services;

public sealed class FastingTelemetryCleanupService(IFastingTelemetryEventWriteRepository repository)
    : IFastingTelemetryCleanupService {
    public async Task<int> CleanupAsync(
        DateTime olderThanUtc,
        int batchSize,
        CancellationToken cancellationToken) {
        int totalDeletedCount = 0;
        int deletedCount;
        do {
            cancellationToken.ThrowIfCancellationRequested();
            deletedCount = await repository
                .DeleteOlderThanAsync(olderThanUtc, batchSize, cancellationToken)
                .ConfigureAwait(false);
            totalDeletedCount += deletedCount;
        } while (deletedCount == batchSize);

        return totalDeletedCount;
    }
}
