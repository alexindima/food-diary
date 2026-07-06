using FoodDiary.Application.Abstractions.Notifications.Models;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Abstractions.Notifications.Common;

public interface IWebPushSubscriptionReadModelRepository {
    Task<IReadOnlyList<WebPushSubscriptionReadModel>> GetByUserReadModelsAsync(
        UserId userId,
        CancellationToken cancellationToken = default);
}