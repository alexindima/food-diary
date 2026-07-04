using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Abstractions.Dietologist.Common;
using FoodDiary.Application.Dietologist.Mappings;
using FoodDiary.Application.Abstractions.Notifications.Common;
using FoodDiary.Application.Notifications.Mappings;
using FoodDiary.Application.Notifications.Models;
using FoodDiary.Application.Users.Common;
using FoodDiary.Application.Users.Mappings;
using FoodDiary.Application.Users.Models;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.Entities.Notifications;
using FoodDiary.Domain.Entities.Dietologist;

namespace FoodDiary.Application.Users.Queries.GetProfileOverview;

public sealed class GetProfileOverviewQueryHandler(
    IUserContextService userContextService,
    IWebPushSubscriptionRepository webPushSubscriptionRepository,
    IDietologistInvitationReadRepository dietologistInvitationRepository)
    : IQueryHandler<GetProfileOverviewQuery, Result<ProfileOverviewModel>> {
    public async Task<Result<ProfileOverviewModel>> Handle(GetProfileOverviewQuery query, CancellationToken cancellationToken) {
        if (query.UserId is null || query.UserId == Guid.Empty) {
            return Result.Failure<ProfileOverviewModel>(Errors.Authentication.InvalidToken);
        }

        var userId = new UserId(query.UserId.Value);
        Result<User> userResult = await userContextService.GetAccessibleUserAsync(userId, cancellationToken).ConfigureAwait(false);
        if (userResult.IsFailure) {
            return Result.Failure<ProfileOverviewModel>(userResult.Error);
        }

        User user = userResult.Value;
        IReadOnlyList<WebPushSubscription> webPushSubscriptions = await webPushSubscriptionRepository.GetByUserAsync(userId, cancellationToken).ConfigureAwait(false);
        DietologistInvitation? acceptedRelationship = await dietologistInvitationRepository.GetActiveByClientAsync(userId, cancellationToken: cancellationToken).ConfigureAwait(false);
        DietologistInvitation? pendingRelationship = acceptedRelationship is null
            ? await dietologistInvitationRepository.GetByClientAndStatusAsync(
                userId,
                DietologistInvitationStatus.Pending,
                cancellationToken: cancellationToken).ConfigureAwait(false)
            : null;

        var notificationPreferences = new NotificationPreferencesModel(
            user.PushNotificationsEnabled,
            user.FastingPushNotificationsEnabled,
            user.SocialPushNotificationsEnabled,
            user.FastingCheckInReminderHours,
            user.FastingCheckInFollowUpReminderHours);

        return Result.Success(new ProfileOverviewModel(
            user.ToModel(),
            notificationPreferences,
            webPushSubscriptions.Select(static subscription => subscription.ToModel()).ToList(),
            acceptedRelationship?.ToRelationshipModel() ?? pendingRelationship?.ToRelationshipModel()));
    }
}
