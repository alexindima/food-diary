using FoodDiary.Modules.Notifications.Domain.Entities;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;

namespace FoodDiary.Modules.Notifications.Application.Abstractions.Common;

public interface IWebPushSubscriptionReadRepository {
    Task<WebPushSubscription?> GetByEndpointAsync(
        string endpoint,
        bool asTracking = false,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<WebPushSubscription>> GetByUserAsync(
        UserId userId,
        CancellationToken cancellationToken = default);
}
