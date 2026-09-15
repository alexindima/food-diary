using FoodDiary.Results;
using FoodDiary.Modules.Dashboard.Application.Abstractions.Models;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;

namespace FoodDiary.Modules.Dashboard.Application.Abstractions.Common;

public interface IDashboardMealsReadService {
    Task<Result<DashboardMealsReadModel>> GetMealsAsync(
        UserId userId,
        int page,
        int limit,
        DateTime dateFrom,
        DateTime dateTo,
        CancellationToken cancellationToken = default);
}
