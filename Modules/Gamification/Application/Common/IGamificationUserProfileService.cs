using FoodDiary.Results;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Gamification.Common;

public interface IGamificationUserProfileService {
    Task<Result<IGamificationUserProfile>> GetAsync(UserId userId, CancellationToken cancellationToken = default);
}
