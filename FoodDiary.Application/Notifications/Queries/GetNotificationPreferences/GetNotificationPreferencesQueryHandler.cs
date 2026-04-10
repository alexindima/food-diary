using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Application.Common.Interfaces.Persistence;
using FoodDiary.Application.Notifications.Models;
using FoodDiary.Application.Users.Common;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Notifications.Queries.GetNotificationPreferences;

public sealed class GetNotificationPreferencesQueryHandler(IUserRepository userRepository)
    : IQueryHandler<GetNotificationPreferencesQuery, Result<NotificationPreferencesModel>> {
    public async Task<Result<NotificationPreferencesModel>> Handle(
        GetNotificationPreferencesQuery query,
        CancellationToken cancellationToken) {
        if (query.UserId is null || query.UserId == Guid.Empty) {
            return Result.Failure<NotificationPreferencesModel>(Errors.Authentication.InvalidToken);
        }

        var user = await userRepository.GetByIdAsync(new UserId(query.UserId.Value), cancellationToken);
        var accessError = CurrentUserAccessPolicy.EnsureCanAccess(user);
        if (accessError is not null) {
            return Result.Failure<NotificationPreferencesModel>(accessError);
        }

        return Result.Success(new NotificationPreferencesModel(
            user!.PushNotificationsEnabled,
            user.FastingPushNotificationsEnabled,
            user.SocialPushNotificationsEnabled,
            user.FastingCheckInReminderHours,
            user.FastingCheckInFollowUpReminderHours));
    }
}
