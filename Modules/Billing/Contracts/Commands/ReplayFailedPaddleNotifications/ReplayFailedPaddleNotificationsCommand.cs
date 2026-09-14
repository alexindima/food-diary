using FoodDiary.Mediator;
using FoodDiary.Modules.Billing.Contracts.Models;

namespace FoodDiary.Modules.Billing.Contracts.Commands.ReplayFailedPaddleNotifications;

public sealed record ReplayFailedPaddleNotificationsCommand : IRequest<PaddleNotificationRecoveryResult>;
