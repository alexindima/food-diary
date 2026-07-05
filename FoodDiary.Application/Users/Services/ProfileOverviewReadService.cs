using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Abstractions.Dietologist.Common;
using FoodDiary.Application.Abstractions.Notifications.Common;
using FoodDiary.Application.Dietologist.Mappings;
using FoodDiary.Application.Notifications.Mappings;
using FoodDiary.Application.Notifications.Models;
using FoodDiary.Application.Users.Common;
using FoodDiary.Application.Users.Models;
using FoodDiary.Domain.Entities.Dietologist;
using FoodDiary.Domain.Entities.Notifications;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Users.Services;

public sealed class ProfileOverviewReadService(
    IUserProfileReadService userProfileReadService,
    IWebPushSubscriptionReadRepository webPushSubscriptionRepository,
    IDietologistInvitationReadRepository dietologistInvitationRepository)
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

        IReadOnlyList<WebPushSubscription> webPushSubscriptions = await webPushSubscriptionRepository.GetByUserAsync(userId, cancellationToken).ConfigureAwait(false);
        DietologistInvitation? acceptedRelationship = await dietologistInvitationRepository.GetActiveByClientAsync(userId, cancellationToken: cancellationToken).ConfigureAwait(false);
        DietologistInvitation? pendingRelationship = acceptedRelationship is null
            ? await dietologistInvitationRepository.GetByClientAndStatusAsync(
                userId,
                DietologistInvitationStatus.Pending,
                cancellationToken: cancellationToken).ConfigureAwait(false)
            : null;

        return Result.Success(new ProfileOverviewModel(
            userResult.Value,
            preferencesResult.Value,
            webPushSubscriptions.Select(static subscription => subscription.ToModel()).ToList(),
            acceptedRelationship?.ToRelationshipModel() ?? pendingRelationship?.ToRelationshipModel()));
    }
}
