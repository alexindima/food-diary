using FoodDiary.Results;
using FoodDiary.Modules.Dashboard.Contracts.Models;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Modules.Dashboard.Application.Abstractions.Common;

public interface IDashboardStatisticsReadService {
    Task<Result<IReadOnlyList<DashboardStatisticsBucketReadModel>>> GetStatisticsAsync(
        UserId userId,
        DateTime dateFrom,
        DateTime dateTo,
        int quantizationDays,
        CancellationToken cancellationToken = default);
}
