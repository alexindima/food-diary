using FoodDiary.Results;
using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;

namespace FoodDiary.Application.Notifications.Commands.RemoveWebPushSubscription;

public sealed record RemoveWebPushSubscriptionCommand(Guid? UserId, string Endpoint) : ICommand<Result>, IUserRequest;
