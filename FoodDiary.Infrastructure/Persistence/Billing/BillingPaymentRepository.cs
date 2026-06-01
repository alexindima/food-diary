using FoodDiary.Application.Abstractions.Billing.Common;
using FoodDiary.Domain.Entities.Billing;
using Microsoft.EntityFrameworkCore;
using Npgsql;

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
        try {
            await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        } catch (DbUpdateException ex) when (IsDuplicatePayment(ex)) {
            context.Entry(payment).State = EntityState.Detached;
            throw new BillingPaymentAlreadyExistsException(payment.Provider, payment.ExternalPaymentId);
        }

        return payment;
    }

    private static bool IsDuplicatePayment(DbUpdateException exception) =>
        exception.InnerException is PostgresException {
            SqlState: PostgresErrorCodes.UniqueViolation,
            ConstraintName: "IX_BillingPayments_Provider_ExternalPaymentId"
        };
}
