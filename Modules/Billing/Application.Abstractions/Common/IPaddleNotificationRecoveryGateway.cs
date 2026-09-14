using FoodDiary.Modules.Billing.Contracts.Models;

namespace FoodDiary.Modules.Billing.Application.Abstractions.Common;

public interface IPaddleNotificationRecoveryGateway {
    Task<PaddleNotificationRecoveryResult> ReplayFailedAsync(CancellationToken cancellationToken = default);
}
