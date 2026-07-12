using FoodDiary.Results;
using FoodDiary.Application.Notifications.Common;
using FoodDiary.Application.Notifications.Models;
using FoodDiary.Application.Users.Common;
using FoodDiary.Application.Users.Models;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Users.Services;

public sealed class ProfileOverviewReadService(
    IUserProfileReadService userProfileReadService,
    IWebPushSubscriptionReadService webPushSubscriptionReadService,
    IProfileDietologistReadService dietologistReadService)
    : IProfileOverviewReadService {
    public async Task<Result<ProfileOverviewModel>> GetAsync(UserId userId, CancellationToken cancellationToken) {
        Result<UserModel> userResult = await userProfileReadService.GetUserAsync(userId, cancellationToken).ConfigureAwait(false);
        if (userResult.IsFailure) {
            return Result.Failure<ProfileOverviewModel>(userResult.Error);
        }

        Result<NotificationPreferencesModel> preferencesResult = await userProfileReadService.GetNotificationPreferencesAsync(userId, cancellationToken).ConfigureAwait(false);
        if (preferencesResult.IsFailure) {
            return Result.Failure<ProfileOverviewModel>(preferencesResult.Error);
        }

        IReadOnlyList<WebPushSubscriptionModel> webPushSubscriptions = await webPushSubscriptionReadService
            .GetSubscriptionsAsync(userId, cancellationToken)
            .ConfigureAwait(false);
        Result<ProfileDietologistRelationshipModel?> relationshipResult = await dietologistReadService
            .GetRelationshipAsync(userId, cancellationToken)
            .ConfigureAwait(false);
        if (relationshipResult.IsFailure) {
            return Result.Failure<ProfileOverviewModel>(relationshipResult.Error);
        }

        return Result.Success(new ProfileOverviewModel(
            userResult.Value,
            preferencesResult.Value,
            webPushSubscriptions,
            relationshipResult.Value));
    }
}
