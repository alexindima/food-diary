using FoodDiary.Results;
using FoodDiary.Modules.Users.Contracts.Common;
using FoodDiary.Modules.Users.Contracts.Models;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;

namespace FoodDiary.Modules.Users.Application.Queries.GetProfileOverview;

public sealed class GetProfileOverviewQueryHandler(
    IUserProfileReadService userProfileReadService,
    IProfileNotificationReadService notificationReadService,
    IProfileDietologistReadService dietologistReadService,
    ICurrentUserAccessService currentUserAccessService)
    : IQueryHandler<GetProfileOverviewQuery, Result<ProfileOverviewModel>> {
    public async Task<Result<ProfileOverviewModel>> Handle(GetProfileOverviewQuery query, CancellationToken cancellationToken) {
        Result<UserId> userIdResult = await CurrentUserAccessResolver.ResolveAsync(
            query.UserId,
            currentUserAccessService,
            cancellationToken).ConfigureAwait(false);
        if (userIdResult.IsFailure) {
            return CurrentUserAccessResolver.ToFailure<ProfileOverviewModel>(userIdResult);
        }

        UserId userId = userIdResult.Value;
        return await GetOverviewAsync(userId, cancellationToken).ConfigureAwait(false);
    }
    private async Task<Result<ProfileOverviewModel>> GetOverviewAsync(UserId userId, CancellationToken cancellationToken) {
        Result<UserModel> userResult = await userProfileReadService.GetUserAsync(userId, cancellationToken).ConfigureAwait(false);
        if (userResult.IsFailure) {
            return Result.Failure<ProfileOverviewModel>(userResult.Error);
        }

        Result<UserNotificationPreferencesModel> preferencesResult = await userProfileReadService.GetNotificationPreferencesAsync(userId, cancellationToken).ConfigureAwait(false);
        if (preferencesResult.IsFailure) {
            return Result.Failure<ProfileOverviewModel>(preferencesResult.Error);
        }

        IReadOnlyList<ProfileWebPushSubscriptionModel> webPushSubscriptions = await notificationReadService
            .GetWebPushSubscriptionsAsync(userId, cancellationToken)
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
