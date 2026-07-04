namespace FoodDiary.Application.Abstractions.Fasting.Common;

public interface IFastingTelemetryEventReadRepository {
    Task<IReadOnlyList<FastingTelemetryEventRecord>> GetSinceAsync(DateTime sinceUtc, CancellationToken cancellationToken = default);
}
