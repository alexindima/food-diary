using FoodDiary.Results;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Application.Dashboard.Models;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Dashboard.Common;

public interface IDashboardUserContextService : ICurrentUserAccessService {
    Task<Result<DashboardUserContextModel>> GetAccessibleDashboardUserAsync(
        UserId userId,
        CancellationToken cancellationToken);
}
