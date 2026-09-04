using FoodDiary.Application.Abstractions.Users.Models;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Results;

namespace FoodDiary.Application.Abstractions.Users.Common;

public interface IUserGamificationProfileReadService {
    Task<Result<UserGamificationProfileModel>> GetGamificationProfileAsync(
        UserId userId,
        CancellationToken cancellationToken = default);
}
