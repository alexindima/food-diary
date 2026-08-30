using FoodDiary.Application.Abstractions.Notifications.Models;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Abstractions.Notifications.Common;

public interface IWebPushDeliveryAudienceService {
    Task<IReadOnlyList<WebPushDeliverySubscription>> GetActiveAudienceAsync(
        UserId userId,
        string notificationType,
        DateTime utcNow,
        CancellationToken cancellationToken);

    Task RemoveInvalidSubscriptionsAsync(
        UserId userId,
        IReadOnlyCollection<Guid> subscriptionIds,
        CancellationToken cancellationToken);
}
