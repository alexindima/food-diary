using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;

namespace FoodDiary.Modules.Notifications.Application.Commands.DeliverTestNotification;

public sealed record DeliverTestNotificationCommand(Guid UserId, string Type) : ICommand<Result>;
