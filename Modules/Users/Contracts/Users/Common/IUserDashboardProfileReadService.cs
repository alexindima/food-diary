using FoodDiary.Application.Abstractions.Users.Models;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Results;

namespace FoodDiary.Application.Abstractions.Users.Common;

public interface IUserDashboardProfileReadService {
    Task<Result<UserDashboardProfileModel>> GetDashboardProfileAsync(
        UserId userId,
        CancellationToken cancellationToken = default);
}
