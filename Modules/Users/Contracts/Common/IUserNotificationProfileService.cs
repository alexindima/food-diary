using FoodDiary.Modules.Users.Contracts.Models;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Results;

namespace FoodDiary.Modules.Users.Contracts.Common;

public interface IUserNotificationProfileService {
    Task<Result<UserNotificationProfileModel>> GetAsync(
        UserId userId,
        CancellationToken cancellationToken = default);

    Task<Result<UserNotificationProfileModel>> UpdatePreferencesAsync(
        UserId userId,
        UserPreferenceUpdate update,
        CancellationToken cancellationToken = default);
}
