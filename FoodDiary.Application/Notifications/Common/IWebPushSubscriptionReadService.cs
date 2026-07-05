using FoodDiary.Application.Notifications.Models;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Notifications.Common;

public interface IWebPushSubscriptionReadService {
    Task<IReadOnlyList<WebPushSubscriptionModel>> GetActiveSubscriptionsAsync(
        UserId userId,
        DateTime utcNow,
        CancellationToken cancellationToken);
}
