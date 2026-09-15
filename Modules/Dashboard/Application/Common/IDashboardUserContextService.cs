using FoodDiary.Results;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Modules.Dashboard.Application.Models;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Modules.Dashboard.Application.Common;

public interface IDashboardUserContextService : ICurrentUserAccessService {
    Task<Result<DashboardUserContextModel>> GetAccessibleDashboardUserAsync(
        UserId userId,
        CancellationToken cancellationToken);
}
