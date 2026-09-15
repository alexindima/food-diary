using FoodDiary.Modules.Users.Contracts.Models;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;

namespace FoodDiary.Modules.Users.Contracts.Common;

public interface IProfileNotificationReadService {
    Task<IReadOnlyList<ProfileWebPushSubscriptionModel>> GetWebPushSubscriptionsAsync(
        UserId userId,
        CancellationToken cancellationToken);
}
