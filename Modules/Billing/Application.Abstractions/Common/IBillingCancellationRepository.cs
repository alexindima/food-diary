using FoodDiary.Modules.Billing.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.Billing.Domain.Entities;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.Billing.Application.Abstractions.Models;

namespace FoodDiary.Modules.Billing.Application.Abstractions.Common;

public interface IBillingCancellationRepository {
    Task<IReadOnlyList<BillingCancellationEmailCandidateModel>> GetPendingConfirmationsAsync(int limit, CancellationToken cancellationToken);
    Task<IReadOnlyList<BillingCancellationTelegramCandidateModel>> GetPendingTelegramConfirmationsAsync(int limit, CancellationToken cancellationToken);
    Task<BillingCancellationTelegramSnapshotModel?> GetTelegramConfirmationAsync(BillingCancellationId id, UserId userId, CancellationToken cancellationToken);
    Task MarkTelegramConfirmationHandledAsync(BillingCancellationId id, UserId userId, DateTime handledAtUtc, CancellationToken cancellationToken);
    Task<BillingCancellation?> GetLatestAsync(UserId userId, string provider, string externalSubscriptionId, CancellationToken cancellationToken);
    Task<BillingCancellation?> GetOwnedAsync(BillingCancellationId id, UserId userId, CancellationToken cancellationToken);
    Task AddAsync(BillingCancellation cancellation, CancellationToken cancellationToken);
    Task UpdateAsync(BillingCancellation cancellation, CancellationToken cancellationToken);
}
