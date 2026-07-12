using FoodDiary.Application.Users.Models;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Users.Common;

public interface IProfileNotificationReadService {
    Task<IReadOnlyList<ProfileWebPushSubscriptionModel>> GetWebPushSubscriptionsAsync(
        UserId userId,
        CancellationToken cancellationToken);
}
