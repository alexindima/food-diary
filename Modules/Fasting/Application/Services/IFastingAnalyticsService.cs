using FoodDiary.Modules.Fasting.Contracts.Read.Models;
using FoodDiary.Application.Abstractions.Common.Models;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Modules.Fasting.Application.Services;

public interface IFastingAnalyticsService {
    (DateTime FromUtc, DateTime ToUtc) GetDefaultHistoryWindow(DateTime nowUtc);

    Task<FastingStatsModel> GetStatsAsync(UserId userId, DateTime nowUtc, CancellationToken cancellationToken);

    Task<FastingInsightsModel> GetInsightsAsync(
        UserId userId,
        DateTime nowUtc,
        FastingOccurrenceReadModel? current,
        CancellationToken cancellationToken);

    Task<PagedResponse<FastingSessionModel>> GetHistoryAsync(
        UserId userId,
        int page,
        int limit,
        DateTime fromUtc,
        DateTime toUtc,
        CancellationToken cancellationToken);
}
