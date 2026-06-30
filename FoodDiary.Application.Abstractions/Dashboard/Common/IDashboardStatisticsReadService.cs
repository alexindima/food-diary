using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Abstractions.Dashboard.Models;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Abstractions.Dashboard.Common;

public interface IDashboardStatisticsReadService {
    Task<Result<IReadOnlyList<DashboardStatisticsBucketReadModel>>> GetStatisticsAsync(
        UserId userId,
        DateTime dateFrom,
        DateTime dateTo,
        int quantizationDays,
        CancellationToken cancellationToken = default);
}
