using System.Data.Common;
using FoodDiary.Persistence.Abstractions;
using FoodDiary.Modules.Billing.Application.Abstractions.Common;
using FoodDiary.Modules.Billing.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace FoodDiary.Modules.Billing.Infrastructure.Persistence;

public sealed class EfBillingTransactionRunner(IModuleTransactionCoordinator coordinator) : IBillingTransactionRunner {
    public Task ExecuteAsync(Func<CancellationToken, Task> operation, CancellationToken cancellationToken = default) =>
        ExecuteCoreAsync(serializationKey: null, operation, cancellationToken);

    public Task ExecuteSerializedAsync(string serializationKey, Func<CancellationToken, Task> operation, CancellationToken cancellationToken = default) {
        ArgumentException.ThrowIfNullOrWhiteSpace(serializationKey);
        return ExecuteCoreAsync(serializationKey, operation, cancellationToken);
    }

    private async Task ExecuteCoreAsync(string? serializationKey, Func<CancellationToken, Task> operation, CancellationToken cancellationToken) {
        ArgumentNullException.ThrowIfNull(operation);
        await coordinator.ExecuteAsync(async (transaction, token) => {
            if (serializationKey is not null) {
                await AcquireTransactionLockAsync(serializationKey, transaction, token).ConfigureAwait(false);
            }
            await operation(token).ConfigureAwait(false);
        }, TranslateException, cancellationToken).ConfigureAwait(false);
    }

    private static Exception TranslateException(Exception exception) {
        if (exception is DbUpdateException update) {
            if (IsDuplicatePayment(update) && DetachAddedPayment(update) is { } payment) {
                return new BillingPaymentAlreadyExistsException(payment.Provider, payment.ExternalPaymentId);
            }
            if (IsDuplicateWebhookEvent(update) && DetachAddedWebhookEvent(update) is { } webhookEvent) {
                return new BillingWebhookEventAlreadyProcessedException(webhookEvent.Provider, webhookEvent.EventId);
            }
        }
        return exception;
    }

    private static async Task AcquireTransactionLockAsync(string serializationKey, DbTransaction transaction, CancellationToken cancellationToken) {
        var connection = (NpgsqlConnection)transaction.Connection!;
        var command = new NpgsqlCommand(
            "SELECT pg_advisory_xact_lock(hashtextextended(@serialization_key, 0))",
            connection, (NpgsqlTransaction)transaction);
        await using (command.ConfigureAwait(false)) {
            command.Parameters.AddWithValue("serialization_key", NpgsqlTypes.NpgsqlDbType.Text, serializationKey);
            await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    private static BillingPayment? DetachAddedPayment(DbUpdateException exception) {
        Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry? entry = exception.Entries
            .FirstOrDefault(candidate => candidate.Entity is BillingPayment && candidate.State == EntityState.Added);
        if (entry is null) {
            return null;
        }

        var payment = (BillingPayment)entry.Entity;
        entry.State = EntityState.Detached;
        return payment;
    }

    private static BillingWebhookEvent? DetachAddedWebhookEvent(DbUpdateException exception) {
        Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry? entry = exception.Entries
            .FirstOrDefault(candidate => candidate.Entity is BillingWebhookEvent && candidate.State == EntityState.Added);
        if (entry is null) {
            return null;
        }

        var webhookEvent = (BillingWebhookEvent)entry.Entity;
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
