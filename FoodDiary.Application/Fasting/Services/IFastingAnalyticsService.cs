using FoodDiary.Application.Common.Models;
using FoodDiary.Application.Fasting.Models;
using FoodDiary.Domain.Entities.Tracking.Fasting;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Fasting.Services;

public interface IFastingAnalyticsService {
    (DateTime FromUtc, DateTime ToUtc) GetDefaultHistoryWindow(DateTime nowUtc);

    Task<FastingStatsModel> GetStatsAsync(UserId userId, DateTime nowUtc, CancellationToken cancellationToken);

    Task<FastingInsightsModel> GetInsightsAsync(
        UserId userId,
        DateTime nowUtc,
        FastingOccurrence? current,
        CancellationToken cancellationToken);

    Task<PagedResponse<FastingSessionModel>> GetHistoryAsync(
        UserId userId,
        int page,
        int limit,
        DateTime fromUtc,
        DateTime toUtc,
        CancellationToken cancellationToken);
}
