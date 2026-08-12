using FoodDiary.Results;
using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Application.Notifications.Models;

namespace FoodDiary.Application.Notifications.Queries.GetWebPushSubscriptions;

public sealed record GetWebPushSubscriptionsQuery(Guid? UserId)
    : IQuery<Result<IReadOnlyList<WebPushSubscriptionModel>>>, IUserRequest;
