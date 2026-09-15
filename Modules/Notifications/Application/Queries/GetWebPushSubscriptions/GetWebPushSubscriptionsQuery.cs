using FoodDiary.Results;
using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Modules.Notifications.Application.Models;

namespace FoodDiary.Modules.Notifications.Application.Queries.GetWebPushSubscriptions;

public sealed record GetWebPushSubscriptionsQuery(Guid? UserId)
    : IQuery<Result<IReadOnlyList<WebPushSubscriptionModel>>>, IUserRequest;
