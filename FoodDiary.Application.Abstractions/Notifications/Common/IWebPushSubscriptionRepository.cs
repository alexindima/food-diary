using FoodDiary.Domain.Entities.Notifications;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Abstractions.Notifications.Common;

public interface IWebPushSubscriptionRepository {
    Task<WebPushSubscription?> GetByEndpointAsync(string endpoint, bool asTracking = false, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<WebPushSubscription>> GetByUserAsync(UserId userId, CancellationToken cancellationToken = default);
    Task<WebPushSubscription> AddAsync(WebPushSubscription subscription, CancellationToken cancellationToken = default);
    Task UpdateAsync(WebPushSubscription subscription, CancellationToken cancellationToken = default);
    Task DeleteAsync(WebPushSubscription subscription, CancellationToken cancellationToken = default);
    Task DeleteRangeAsync(IReadOnlyCollection<WebPushSubscription> subscriptions, CancellationToken cancellationToken = default);
}
