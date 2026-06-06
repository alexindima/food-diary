using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Notifications.Models;

namespace FoodDiary.Application.Notifications.Queries.GetWebPushSubscriptions;

public sealed record GetWebPushSubscriptionsQuery(Guid? UserId)
    : IQuery<Result<IReadOnlyList<WebPushSubscriptionModel>>>, IUserRequest;
