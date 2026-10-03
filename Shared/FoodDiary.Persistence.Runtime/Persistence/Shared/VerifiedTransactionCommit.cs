using System.Runtime.ExceptionServices;
using Microsoft.EntityFrameworkCore.Storage;
using Npgsql;

namespace FoodDiary.Persistence.Runtime.Persistence.Shared;

/// <summary>Replays a transaction only when PostgreSQL confirms that its COMMIT did not succeed.</summary>
internal static class VerifiedTransactionCommit {
    public static async Task CommitAsync(IDbContextTransaction transaction, CancellationToken cancellationToken) {
        var providerTransaction = transaction.GetDbTransaction() as NpgsqlTransaction;
        NpgsqlConnection? source = providerTransaction?.Connection;
        string? transactionId = null;
        if (source is not null) {
            var identify = new NpgsqlCommand("select pg_current_xact_id()::text", source, providerTransaction);
            await using (identify.ConfigureAwait(false)) {
                transactionId = (string?)await identify.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
            }
        }
        try {
            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
        } catch (Exception commitException) {
            // Dispose first: a COMMIT cancelled before being sent may still hold a live transaction.
            // A completed COMMIT cannot be undone by this disposal.
            try {
                await transaction.DisposeAsync().ConfigureAwait(false);
            } catch {
                // Verification, rather than disposal, determines the outcome.
            }
            if (source is null || transactionId is null) {
                throw new CommitOutcomeUnknownException(commitException);
            }
            string? status;
            try {
                using var verificationTimeout = new CancellationTokenSource(TimeSpan.FromSeconds(15));
                NpgsqlConnection verification = await source.CloneWithAsync(source.ConnectionString, verificationTimeout.Token).ConfigureAwait(false);
                await using (verification.ConfigureAwait(false)) {
                    await verification.OpenAsync(verificationTimeout.Token).ConfigureAwait(false);
                    var check = new NpgsqlCommand("select pg_xact_status(cast(@transactionId as xid8))", verification);
                    await using (check.ConfigureAwait(false)) {
                        check.Parameters.AddWithValue("transactionId", transactionId);
                        status = (string?)await check.ExecuteScalarAsync(verificationTimeout.Token).ConfigureAwait(false);
                    }
                }
            } catch (Exception verificationException) {
                throw new CommitOutcomeUnknownException(new AggregateException(commitException, verificationException));
            }
            if (string.Equals(status, "committed", StringComparison.Ordinal)) {
                return;
            }
            if (string.Equals(status, "aborted", StringComparison.Ordinal)) {
                ExceptionDispatchInfo.Capture(commitException).Throw();
            }
            // An unavailable or still-running outcome is never permission to replay the handler.
            throw new CommitOutcomeUnknownException(commitException);
        }
    }
}
