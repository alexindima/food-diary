using FoodDiary.Application.Abstractions.Dashboard.Models;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Abstractions.Dashboard.Common;

public interface IDashboardBodyReadService {
    Task<DashboardBodyReadModel> GetBodyAsync(
        UserId userId,
        DateTime dayStart,
        DateTime dayEndStart,
        DateTime trendStart,
        int trendQuantizationDays,
        bool includeWeight,
        bool includeWaist,
        bool includeHydration,
        CancellationToken cancellationToken = default);
}
