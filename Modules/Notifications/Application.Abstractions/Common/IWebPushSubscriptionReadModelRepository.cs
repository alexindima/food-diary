using FoodDiary.Modules.Notifications.Application.Abstractions.Models;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Modules.Notifications.Application.Abstractions.Common;

public interface IWebPushSubscriptionReadModelRepository {
    Task<IReadOnlyList<WebPushSubscriptionReadModel>> GetByUserReadModelsAsync(
        UserId userId,
        CancellationToken cancellationToken = default);
}
