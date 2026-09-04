using FoodDiary.Application.Abstractions.Users.Models;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Abstractions.Users.Common;

public interface IProfileNotificationReadService {
    Task<IReadOnlyList<ProfileWebPushSubscriptionModel>> GetWebPushSubscriptionsAsync(
        UserId userId,
        CancellationToken cancellationToken);
}
