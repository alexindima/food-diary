using FoodDiary.Modules.Billing.Application.Abstractions.Common;
using FoodDiary.Modules.Billing.Domain.Entities;
using FoodDiary.Modules.Billing.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Modules.Billing.Infrastructure.Persistence;

public sealed class BillingPaymentRepository(DbSet<BillingPayment> payments, Func<CancellationToken, Task>? synchronizeTransactionAsync = null) : IBillingPaymentReadRepository, IBillingPaymentWriteRepository {
    public async Task<BillingPayment?> GetLatestCheckoutAsync(UserId userId, BillingSubscriptionId subscriptionId,
        string provider, CancellationToken cancellationToken = default) {
        await SynchronizeTransactionAsync(cancellationToken);
        return await payments.Where(payment => payment.UserId == userId && payment.BillingSubscriptionId == subscriptionId.Value &&
                payment.Provider == provider && payment.Kind == BillingPaymentKinds.Checkout &&
                payment.Status == BillingSubscription.PendingCheckoutStatus)
            .OrderByDescending(payment => payment.CreatedOnUtc).ThenByDescending(payment => payment.Id)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<BillingPayment?> GetByExternalPaymentIdAsync(
        string provider,
        string externalPaymentId,
        CancellationToken cancellationToken = default) {
        await SynchronizeTransactionAsync(cancellationToken);
        return await payments
            .FirstOrDefaultAsync(
                payment => payment.Provider == provider && payment.ExternalPaymentId == externalPaymentId,
                cancellationToken);
    }

    public Task<BillingPayment> AddAsync(BillingPayment payment, CancellationToken cancellationToken = default) {
        payments.Add(payment);
        return Task.FromResult(payment);
    }

    public Task UpdateAsync(BillingPayment payment, CancellationToken cancellationToken = default) {
        payments.Update(payment);
        return Task.CompletedTask;
    }

    private Task SynchronizeTransactionAsync(CancellationToken cancellationToken) =>
        synchronizeTransactionAsync?.Invoke(cancellationToken) ?? Task.CompletedTask;
}
