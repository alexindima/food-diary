using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Result;
using FoodDiary.Application.Abstractions.Common.Interfaces.Persistence;
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

namespace FoodDiary.Application.Users.Queries.GetProfileOverview;

public sealed class GetProfileOverviewQueryHandler(
    IUserRepository userRepository,
    IWebPushSubscriptionRepository webPushSubscriptionRepository,
    IDietologistInvitationRepository dietologistInvitationRepository)
    : IQueryHandler<GetProfileOverviewQuery, Result<ProfileOverviewModel>> {
    public async Task<Result<ProfileOverviewModel>> Handle(GetProfileOverviewQuery query, CancellationToken cancellationToken) {
        if (query.UserId is null || query.UserId == Guid.Empty) {
            return Result.Failure<ProfileOverviewModel>(Errors.Authentication.InvalidToken);
        }

        var userId = new UserId(query.UserId.Value);
        var user = await userRepository.GetByIdAsync(userId, cancellationToken);
        var accessError = CurrentUserAccessPolicy.EnsureCanAccess(user);
        if (accessError is not null) {
            return Result.Failure<ProfileOverviewModel>(accessError);
        }

        var webPushSubscriptions = await webPushSubscriptionRepository.GetByUserAsync(userId, cancellationToken);
        var acceptedRelationship = await dietologistInvitationRepository.GetActiveByClientAsync(userId, cancellationToken: cancellationToken);
        var pendingRelationship = acceptedRelationship is null
            ? await dietologistInvitationRepository.GetByClientAndStatusAsync(
                userId,
                DietologistInvitationStatus.Pending,
                cancellationToken: cancellationToken)
            : null;

        var notificationPreferences = new NotificationPreferencesModel(
            user!.PushNotificationsEnabled,
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
