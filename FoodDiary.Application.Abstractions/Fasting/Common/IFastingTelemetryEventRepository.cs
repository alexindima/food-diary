namespace FoodDiary.Application.Fasting.Common;

public interface IFastingTelemetryEventRepository {
    Task AddAsync(FastingTelemetryEventRecord record, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<FastingTelemetryEventRecord>> GetSinceAsync(DateTime sinceUtc, CancellationToken cancellationToken = default);
}
