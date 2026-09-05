using FoodDiary.Application.Abstractions.Billing.Common;
using FoodDiary.Domain.Entities.Billing;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Modules.Billing.Infrastructure.Persistence;

public sealed class BillingPaymentRepository(DbSet<BillingPayment> payments) : IBillingPaymentRepository {
    public Task<BillingPayment?> GetByExternalPaymentIdAsync(
        string provider,
        string externalPaymentId,
        CancellationToken cancellationToken = default) {
        return payments
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
}
