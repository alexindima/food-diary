using FoodDiary.Mediator;
using FoodDiary.Modules.Notifications.Contracts.Common;

namespace FoodDiary.Modules.Notifications.Contracts.Commands.CleanupExpiredNotifications;

public sealed record CleanupExpiredNotificationsCommand(NotificationCleanupPolicy Policy) : IRequest<int>;
