using FoodDiary.Modules.Notifications.Domain.Entities;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;

namespace FoodDiary.Modules.Notifications.Application.Abstractions.Common;

public interface IWebPushSubscriptionWriteRepository {
    Task<WebPushSubscription?> GetByEndpointAsync(
        string endpoint,
        bool asTracking = false,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<WebPushSubscription>> GetByUserAsync(
        UserId userId,
        CancellationToken cancellationToken = default);

    Task<WebPushSubscription> AddAsync(WebPushSubscription subscription, CancellationToken cancellationToken = default);

    Task UpdateAsync(WebPushSubscription subscription, CancellationToken cancellationToken = default);

    Task DeleteAsync(WebPushSubscription subscription, CancellationToken cancellationToken = default);

    Task DeleteRangeAsync(IReadOnlyCollection<WebPushSubscription> subscriptions, CancellationToken cancellationToken = default);
}
