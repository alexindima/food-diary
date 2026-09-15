using FoodDiary.Modules.Notifications.Application.Models;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Modules.Notifications.Application.Common;

public interface IWebPushSubscriptionReadService {
    Task<IReadOnlyList<WebPushSubscriptionModel>> GetSubscriptionsAsync(
        UserId userId,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<WebPushSubscriptionModel>> GetActiveSubscriptionsAsync(
        UserId userId,
        DateTime utcNow,
        CancellationToken cancellationToken);
}
