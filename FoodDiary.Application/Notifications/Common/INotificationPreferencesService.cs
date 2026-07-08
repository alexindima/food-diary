using FoodDiary.Results;
using FoodDiary.Application.Notifications.Models;
using FoodDiary.Domain.ValueObjects;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Notifications.Common;

public interface INotificationPreferencesService {
    Task<Result<NotificationPreferencesModel>> GetAsync(UserId userId, CancellationToken cancellationToken = default);

    Task<Result<NotificationPreferencesUpdateResult>> UpdateAsync(
        UserId userId,
        UserPreferenceUpdate update,
        CancellationToken cancellationToken = default);
}
