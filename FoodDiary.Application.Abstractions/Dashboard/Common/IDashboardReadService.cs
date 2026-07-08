using FoodDiary.Results;
using FoodDiary.Application.Abstractions.Dashboard.Models;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Abstractions.Dashboard.Common;

public interface IDashboardReadService {
    Task<Result<DashboardReadModel>> GetSnapshotDataAsync(
        UserId userId,
        DateTime dayStart,
        DateTime dayEnd,
        DateTime trendStart,
        int periodDays,
        int page,
        int pageSize,
        DashboardReadSections sections,
        CancellationToken cancellationToken = default);
}
