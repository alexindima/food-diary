using FoodDiary.Results;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.WeeklyCheckIn.Common;

public interface IWeeklyCheckInUserProfileService : ICurrentUserAccessService {
    Task<Result<WeeklyCheckInUserProfile>> GetAsync(UserId userId, CancellationToken cancellationToken = default);
}
