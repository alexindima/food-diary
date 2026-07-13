using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Results;

namespace FoodDiary.Application.Notifications.Commands.DeliverTestNotification;

public sealed record DeliverTestNotificationCommand(Guid UserId, string Type) : ICommand<Result>;
