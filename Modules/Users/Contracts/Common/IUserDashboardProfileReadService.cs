using FoodDiary.Modules.Users.Contracts.Models;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Results;

namespace FoodDiary.Modules.Users.Contracts.Common;

public interface IUserDashboardProfileReadService {
    Task<Result<UserDashboardProfileModel>> GetDashboardProfileAsync(
        UserId userId,
        CancellationToken cancellationToken = default);
}
