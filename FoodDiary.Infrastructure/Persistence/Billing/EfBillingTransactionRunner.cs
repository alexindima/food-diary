using FoodDiary.Application.Abstractions.Billing.Common;
using FoodDiary.Domain.Entities.Billing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Npgsql;

namespace FoodDiary.Infrastructure.Persistence.Billing;

public sealed class EfBillingTransactionRunner(FoodDiaryDbContext context) : IBillingTransactionRunner {
    public Task ExecuteAsync(Func<CancellationToken, Task> operation, CancellationToken cancellationToken = default) =>
        ExecuteCoreAsync(serializationKey: null, operation, cancellationToken);

    public Task ExecuteSerializedAsync(
        string serializationKey,
        Func<CancellationToken, Task> operation,
        CancellationToken cancellationToken = default) {
        ArgumentException.ThrowIfNullOrWhiteSpace(serializationKey);
        return ExecuteCoreAsync(serializationKey, operation, cancellationToken);
    }

    private async Task ExecuteCoreAsync(
        string? serializationKey,
        Func<CancellationToken, Task> operation,
        CancellationToken cancellationToken) {
        IExecutionStrategy strategy = context.Database.CreateExecutionStrategy();
        try {
            await strategy.ExecuteAsync(async () => {
                IDbContextTransaction transaction = await context.Database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
                await using (transaction.ConfigureAwait(false)) {
                    if (serializationKey is not null) {
                        await AcquireTransactionLockAsync(serializationKey, transaction, cancellationToken).ConfigureAwait(false);
                    }

                    await operation(cancellationToken).ConfigureAwait(false);
                    await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                    await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
                }
            }).ConfigureAwait(false);
        } catch (DbUpdateException ex) when (IsDuplicatePayment(ex)) {
            BillingPayment? payment = DetachAddedPayment();
            if (payment is null) {
                throw;
            }

            context.ChangeTracker.Clear();
            throw new BillingPaymentAlreadyExistsException(payment.Provider, payment.ExternalPaymentId);
        } catch (DbUpdateException ex) when (IsDuplicateWebhookEvent(ex)) {
            BillingWebhookEvent? webhookEvent = DetachAddedWebhookEvent();
            if (webhookEvent is null) {
                throw;
            }

            context.ChangeTracker.Clear();
            throw new BillingWebhookEventAlreadyProcessedException(webhookEvent.Provider, webhookEvent.EventId);
        }
    }

    private async Task AcquireTransactionLockAsync(
        string serializationKey,
        IDbContextTransaction transaction,
        CancellationToken cancellationToken) {
        var connection = (NpgsqlConnection)context.Database.GetDbConnection();
        var command = new NpgsqlCommand(
            "SELECT pg_advisory_xact_lock(hashtextextended(@serialization_key, 0))",
            connection,
            (NpgsqlTransaction)transaction.GetDbTransaction());
        await using (command.ConfigureAwait(false)) {
            command.Parameters.AddWithValue("serialization_key", NpgsqlTypes.NpgsqlDbType.Text, serializationKey);
            await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    private BillingPayment? DetachAddedPayment() {
        Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry<BillingPayment>? entry = context.ChangeTracker
            .Entries<BillingPayment>()
            .FirstOrDefault(candidate => candidate.State == EntityState.Added);
        if (entry is null) {
            return null;
        }

        BillingPayment payment = entry.Entity;
        entry.State = EntityState.Detached;
        return payment;
    }

    private BillingWebhookEvent? DetachAddedWebhookEvent() {
        Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry<BillingWebhookEvent>? entry = context.ChangeTracker
            .Entries<BillingWebhookEvent>()
            .FirstOrDefault(candidate => candidate.State == EntityState.Added);
        if (entry is null) {
            return null;
        }

        BillingWebhookEvent webhookEvent = entry.Entity;
        entry.State = EntityState.Detached;
        return webhookEvent;
    }

    private static bool IsDuplicatePayment(DbUpdateException exception) =>
        exception.InnerException is PostgresException {
            SqlState: PostgresErrorCodes.UniqueViolation,
            ConstraintName: "IX_BillingPayments_Provider_ExternalPaymentId",
        };

    private static bool IsDuplicateWebhookEvent(DbUpdateException exception) =>
        exception.InnerException is PostgresException {
            SqlState: PostgresErrorCodes.UniqueViolation,
            ConstraintName: "IX_BillingWebhookEvents_Provider_EventId",
        };
}
