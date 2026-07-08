using FoodDiary.Results;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.WeeklyCheckIn.Common;

public interface IWeeklyCheckInUserProfileService {
    Task<Result<WeeklyCheckInUserProfile>> GetAsync(UserId userId, CancellationToken cancellationToken = default);
}
