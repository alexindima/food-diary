using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Abstractions.Dietologist.Common;
using FoodDiary.Application.Abstractions.Dietologist.Models;
using FoodDiary.Application.Abstractions.Notifications.Common;
using FoodDiary.Application.Abstractions.Notifications.Models;
using FoodDiary.Application.Dietologist.Mappings;
using FoodDiary.Application.Notifications.Mappings;
using FoodDiary.Application.Notifications.Models;
using FoodDiary.Application.Users.Common;
using FoodDiary.Application.Users.Models;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Users.Services;

public sealed class ProfileOverviewReadService(
    IUserProfileReadService userProfileReadService,
    IWebPushSubscriptionReadRepository webPushSubscriptionRepository,
    IDietologistInvitationReadModelRepository dietologistInvitationRepository)
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

        IReadOnlyList<WebPushSubscriptionReadModel> webPushSubscriptions = await webPushSubscriptionRepository.GetByUserReadModelsAsync(userId, cancellationToken).ConfigureAwait(false);
        DietologistInvitationReadModel? acceptedRelationship = await dietologistInvitationRepository.GetActiveByClientReadModelAsync(userId, cancellationToken).ConfigureAwait(false);
        DietologistInvitationReadModel? pendingRelationship = acceptedRelationship is null
            ? await dietologistInvitationRepository.GetByClientAndStatusReadModelAsync(
                userId,
                DietologistInvitationStatus.Pending,
                cancellationToken).ConfigureAwait(false)
            : null;

        return Result.Success(new ProfileOverviewModel(
            userResult.Value,
            preferencesResult.Value,
            webPushSubscriptions.Select(static subscription => subscription.ToModel()).ToList(),
            acceptedRelationship?.ToRelationshipModel() ?? pendingRelationship?.ToRelationshipModel()));
    }
}
