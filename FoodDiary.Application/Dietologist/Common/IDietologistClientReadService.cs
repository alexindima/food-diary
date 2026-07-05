using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Dashboard.Models;
using FoodDiary.Application.Users.Models;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Dietologist.Common;

public interface IDietologistClientReadService {
    Task<Result<DashboardSnapshotModel>> GetDashboardAsync(
        UserId dietologistUserId,
        Guid clientUserId,
        DateTime date,
        DateTime? dateTo,
        string locale,
        int trendDays,
        int page,
        int pageSize,
        CancellationToken cancellationToken);

    Task<Result<UserModel>> GetGoalsAsync(
        UserId dietologistUserId,
        Guid clientUserId,
        CancellationToken cancellationToken);
}
