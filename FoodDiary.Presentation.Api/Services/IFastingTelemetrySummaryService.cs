using FoodDiary.Presentation.Api.Features.Logs.Requests;

namespace FoodDiary.Presentation.Api.Services;

public interface IFastingTelemetrySummaryService {
    Task RecordAsync(ClientTelemetryLogHttpRequest request, CancellationToken cancellationToken);
    Task<FastingTelemetrySummarySnapshot> GetSummaryAsync(int windowHours, CancellationToken cancellationToken);
}
