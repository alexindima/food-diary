using FoodDiary.Results;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Tdee.Common;

public interface ITdeeUserProfileService {
    Task<Result<TdeeUserProfile>> GetAsync(UserId userId, CancellationToken cancellationToken = default);
}
