using FoodDiary.Modules.Billing.Application.Abstractions.Common;
using FoodDiary.Modules.Billing.Application.Abstractions.Models;
using FoodDiary.Modules.Billing.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.Billing.Domain.Entities;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using Microsoft.EntityFrameworkCore;
using FoodDiary.Modules.Billing.Domain.Contracts;

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

    public async Task<IReadOnlyList<BillingCancellationTelegramCandidateModel>> GetPendingTelegramConfirmationsAsync(int limit, CancellationToken cancellationToken) {
        await synchronizeTransactionAsync(cancellationToken);
        return await cancellations.AsNoTracking()
            .Where(item => item.Provider == BillingProviderNames.TelegramStars && item.ConfirmationTelegramHandledAtUtc == null)
            .OrderBy(item => item.RequestedAtUtc).ThenBy(item => item.Id)
            .Take(Math.Clamp(limit, 1, 100))
            .Select(item => new BillingCancellationTelegramCandidateModel(new BillingCancellationId(item.Id), item.UserId))
            .ToListAsync(cancellationToken);
    }

    public async Task<BillingCancellationTelegramSnapshotModel?> GetTelegramConfirmationAsync(BillingCancellationId id, UserId userId, CancellationToken cancellationToken) {
        await synchronizeTransactionAsync(cancellationToken);
        // Projection reads must observe state committed while a previous provider call was in flight.
        return await cancellations.AsNoTracking().Where(item => item.Id == id.Value && item.UserId == userId)
            .Select(item => new BillingCancellationTelegramSnapshotModel(item.Provider, item.ExternalSubscriptionId,
                item.Outcome, item.EffectiveAtUtc, item.ConfirmationTelegramHandledAtUtc))
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task MarkTelegramConfirmationHandledAsync(BillingCancellationId id, UserId userId, DateTime handledAtUtc, CancellationToken cancellationToken) {
        if (handledAtUtc.Kind != DateTimeKind.Utc) {
            throw new ArgumentException("A UTC instant is required.", nameof(handledAtUtc));
        }
        await synchronizeTransactionAsync(cancellationToken);
        // Do not save a pre-HTTP aggregate snapshot over concurrent cancellation, feedback or email updates.
        await cancellations.Where(item => item.Id == id.Value && item.UserId == userId && item.ConfirmationTelegramHandledAtUtc == null)
            .ExecuteUpdateAsync(setters => setters.SetProperty(item => item.ConfirmationTelegramHandledAtUtc, handledAtUtc), cancellationToken);
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
