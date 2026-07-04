namespace FoodDiary.Application.Abstractions.Fasting.Common;

public interface IFastingTelemetryEventWriteRepository {
    Task AddAsync(FastingTelemetryEventRecord record, CancellationToken cancellationToken = default);
}
