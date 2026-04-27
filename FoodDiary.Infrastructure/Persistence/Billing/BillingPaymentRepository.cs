using FoodDiary.Application.Abstractions.Billing.Common;
using FoodDiary.Domain.Entities.Billing;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Infrastructure.Persistence.Billing;

public sealed class BillingPaymentRepository(FoodDiaryDbContext context) : IBillingPaymentRepository {
    public Task<BillingPayment?> GetByExternalPaymentIdAsync(
        string provider,
        string externalPaymentId,
        CancellationToken cancellationToken = default) {
        return context.BillingPayments
            .FirstOrDefaultAsync(
                payment => payment.Provider == provider && payment.ExternalPaymentId == externalPaymentId,
                cancellationToken);
    }

    public async Task<BillingPayment> AddAsync(BillingPayment payment, CancellationToken cancellationToken = default) {
        context.BillingPayments.Add(payment);
        await context.SaveChangesAsync(cancellationToken);
        return payment;
    }
}
