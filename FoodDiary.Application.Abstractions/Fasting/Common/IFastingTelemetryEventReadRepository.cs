namespace FoodDiary.Application.Abstractions.Fasting.Common;

public interface IFastingTelemetryEventReadRepository {
    Task<IReadOnlyList<FastingTelemetryEventRecord>> GetRangeAsync(
        DateTime fromUtc,
        DateTime toUtc,
        CancellationToken cancellationToken = default);
}
