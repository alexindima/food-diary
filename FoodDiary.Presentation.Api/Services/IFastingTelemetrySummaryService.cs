namespace FoodDiary.Presentation.Api.Services;

public interface IFastingTelemetrySummaryService {
    Task<FastingTelemetrySummarySnapshot> GetSummaryAsync(int windowHours, CancellationToken cancellationToken);
}
