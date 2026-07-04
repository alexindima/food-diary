using FoodDiary.Domain.Entities.Notifications;

namespace FoodDiary.Application.Abstractions.Notifications.Common;

public interface IWebPushSubscriptionWriteRepository : IWebPushSubscriptionReadRepository {
    Task<WebPushSubscription> AddAsync(WebPushSubscription subscription, CancellationToken cancellationToken = default);

    Task UpdateAsync(WebPushSubscription subscription, CancellationToken cancellationToken = default);

    Task DeleteAsync(WebPushSubscription subscription, CancellationToken cancellationToken = default);

    Task DeleteRangeAsync(IReadOnlyCollection<WebPushSubscription> subscriptions, CancellationToken cancellationToken = default);
}
