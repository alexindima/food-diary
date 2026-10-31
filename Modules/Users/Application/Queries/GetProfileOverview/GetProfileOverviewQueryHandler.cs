using FoodDiary.Results;
using FoodDiary.Modules.Users.Contracts.Common;
using FoodDiary.Modules.Users.Contracts.Models;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Application.Contracts.Common.Abstractions.Messaging;

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
            cancellationToken);
        if (userIdResult.IsFailure) {
            return CurrentUserAccessResolver.ToFailure<ProfileOverviewModel>(userIdResult);
        }

        UserId userId = userIdResult.Value;
        return await GetOverviewAsync(userId, cancellationToken);
    }
    private async Task<Result<ProfileOverviewModel>> GetOverviewAsync(UserId userId, CancellationToken cancellationToken) {
        Result<UserModel> userResult = await userProfileReadService.GetUserAsync(userId, cancellationToken);
        if (userResult.IsFailure) {
            return Result.Failure<ProfileOverviewModel>(userResult.Error);
        }

        Result<UserNotificationPreferencesModel> preferencesResult = await userProfileReadService.GetNotificationPreferencesAsync(userId, cancellationToken);
        if (preferencesResult.IsFailure) {
            return Result.Failure<ProfileOverviewModel>(preferencesResult.Error);
        }

        IReadOnlyList<ProfileWebPushSubscriptionModel> webPushSubscriptions = await notificationReadService
            .GetWebPushSubscriptionsAsync(userId, cancellationToken);
        Result<ProfileDietologistRelationshipModel?> relationshipResult = await dietologistReadService
            .GetRelationshipAsync(userId, cancellationToken);
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
