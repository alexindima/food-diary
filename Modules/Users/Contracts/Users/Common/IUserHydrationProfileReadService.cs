using FoodDiary.Application.Abstractions.Users.Models;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Results;

namespace FoodDiary.Application.Abstractions.Users.Common;

public interface IUserHydrationProfileReadService {
    Task<Result<UserHydrationProfileModel>> GetHydrationProfileAsync(
        UserId userId,
        CancellationToken cancellationToken = default);
}
