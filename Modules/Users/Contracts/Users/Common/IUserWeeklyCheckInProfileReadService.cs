using FoodDiary.Application.Abstractions.Users.Models;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Results;

namespace FoodDiary.Application.Abstractions.Users.Common;

public interface IUserWeeklyCheckInProfileReadService {
    Task<Result<UserWeeklyCheckInProfileModel>> GetWeeklyCheckInProfileAsync(
        UserId userId,
        CancellationToken cancellationToken = default);
}
