using FoodDiary.Application.Abstractions.Billing.Common;
using FoodDiary.Domain.Entities.Billing;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Modules.Billing.Infrastructure.Persistence;

public sealed class BillingPaymentRepository(DbSet<BillingPayment> payments, Func<CancellationToken, Task>? synchronizeTransactionAsync = null) : IBillingPaymentRepository {
    public async Task<BillingPayment?> GetByExternalPaymentIdAsync(
        string provider,
        string externalPaymentId,
        CancellationToken cancellationToken = default) {
        await SynchronizeTransactionAsync(cancellationToken).ConfigureAwait(false);
        return await payments
            .FirstOrDefaultAsync(
                payment => payment.Provider == provider && payment.ExternalPaymentId == externalPaymentId,
                cancellationToken).ConfigureAwait(false);
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
