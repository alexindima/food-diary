using FoodDiary.Results;
using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;

namespace FoodDiary.Modules.Notifications.Application.Commands.RemoveWebPushSubscription;

public sealed record RemoveWebPushSubscriptionCommand(Guid? UserId, string Endpoint) : ICommand<Result>, IUserRequest;
