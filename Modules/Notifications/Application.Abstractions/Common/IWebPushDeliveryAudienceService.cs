using FoodDiary.Modules.Notifications.Application.Abstractions.Models;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;

namespace FoodDiary.Modules.Notifications.Application.Abstractions.Common;

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
