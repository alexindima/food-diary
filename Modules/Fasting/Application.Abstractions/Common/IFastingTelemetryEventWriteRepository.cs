namespace FoodDiary.Modules.Fasting.Application.Abstractions.Common;

public interface IFastingTelemetryEventWriteRepository {
    Task AddAsync(FastingTelemetryEventRecord record, CancellationToken cancellationToken = default);

    Task<int> DeleteOlderThanAsync(
        DateTime olderThanUtc,
        int batchSize,
        CancellationToken cancellationToken = default);
}
