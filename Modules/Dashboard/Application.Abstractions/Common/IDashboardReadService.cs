using FoodDiary.Results;
using FoodDiary.Modules.Dashboard.Application.Abstractions.Models;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;

namespace FoodDiary.Modules.Dashboard.Application.Abstractions.Common;

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
        CancellationToken cancellationToken = default,
        DashboardCalendarRange? calendar = null);
}
