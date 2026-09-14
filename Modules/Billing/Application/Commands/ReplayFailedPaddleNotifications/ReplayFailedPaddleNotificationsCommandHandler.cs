using FoodDiary.Mediator;
using FoodDiary.Modules.Billing.Application.Abstractions.Common;
using FoodDiary.Modules.Billing.Contracts.Commands.ReplayFailedPaddleNotifications;
using FoodDiary.Modules.Billing.Contracts.Models;

namespace FoodDiary.Modules.Billing.Application.Commands.ReplayFailedPaddleNotifications;

public sealed class ReplayFailedPaddleNotificationsCommandHandler(IPaddleNotificationRecoveryGateway gateway)
    : IRequestHandler<ReplayFailedPaddleNotificationsCommand, PaddleNotificationRecoveryResult> {
    public Task<PaddleNotificationRecoveryResult> Handle(ReplayFailedPaddleNotificationsCommand request, CancellationToken cancellationToken) =>
        gateway.ReplayFailedAsync(cancellationToken);
}
