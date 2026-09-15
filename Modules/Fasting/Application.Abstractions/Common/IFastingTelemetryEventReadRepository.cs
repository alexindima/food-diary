namespace FoodDiary.Modules.Fasting.Application.Abstractions.Common;

public interface IFastingTelemetryEventReadRepository {
    Task<IReadOnlyList<FastingTelemetryEventRecord>> GetRangeAsync(
        DateTime fromUtc,
        DateTime toUtc,
        CancellationToken cancellationToken = default);
}
