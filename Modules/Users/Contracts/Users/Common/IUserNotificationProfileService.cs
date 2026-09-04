using FoodDiary.Application.Abstractions.Users.Models;
using FoodDiary.Domain.ValueObjects;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Results;

namespace FoodDiary.Application.Abstractions.Users.Common;

public interface IUserNotificationProfileService {
    Task<Result<UserNotificationProfileModel>> GetAsync(
        UserId userId,
        CancellationToken cancellationToken = default);

    Task<Result<UserNotificationProfileModel>> UpdatePreferencesAsync(
        UserId userId,
        UserPreferenceUpdate update,
        CancellationToken cancellationToken = default);
}
