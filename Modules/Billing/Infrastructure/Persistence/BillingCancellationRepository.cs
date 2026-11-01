using FoodDiary.Modules.Billing.Application.Abstractions.Common;
using FoodDiary.Modules.Billing.Application.Abstractions.Models;
using FoodDiary.Modules.Billing.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.Billing.Domain.Entities;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Modules.Billing.Infrastructure.Persistence;

public sealed class BillingCancellationRepository(DbSet<BillingCancellation> cancellations,
    Func<CancellationToken, Task> synchronizeTransactionAsync) : IBillingCancellationRepository {
    public async Task<IReadOnlyList<BillingCancellationEmailCandidateModel>> GetPendingConfirmationsAsync(int limit, CancellationToken cancellationToken) {
        await synchronizeTransactionAsync(cancellationToken);
        return await cancellations.AsNoTracking()
            .Where(item => item.ConfirmationEmailHandledAtUtc == null)
            .OrderBy(item => item.RequestedAtUtc).ThenBy(item => item.Id)
            .Take(Math.Clamp(limit, 1, 100))
            .Select(item => new BillingCancellationEmailCandidateModel(new BillingCancellationId(item.Id), item.UserId))
            .ToListAsync(cancellationToken);
    }

    public async Task<BillingCancellation?> GetLatestAsync(UserId userId, string provider, string externalSubscriptionId, CancellationToken cancellationToken) {
        await synchronizeTransactionAsync(cancellationToken);
        return await cancellations.Where(item => item.UserId == userId && item.Provider == provider &&
                item.ExternalSubscriptionId == externalSubscriptionId)
            .OrderByDescending(item => item.RequestedAtUtc).ThenByDescending(item => item.Id)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<BillingCancellation?> GetOwnedAsync(BillingCancellationId id, UserId userId, CancellationToken cancellationToken) {
        await synchronizeTransactionAsync(cancellationToken);
        return await cancellations.FirstOrDefaultAsync(item => item.Id == id.Value && item.UserId == userId, cancellationToken);
    }

    public async Task AddAsync(BillingCancellation cancellation, CancellationToken cancellationToken) =>
        await cancellations.AddAsync(cancellation, cancellationToken);

    public Task UpdateAsync(BillingCancellation cancellation, CancellationToken cancellationToken) {
        if (cancellations.Entry(cancellation).State != EntityState.Added) {
            cancellations.Update(cancellation);
        }
        return Task.CompletedTask;
    }
}
