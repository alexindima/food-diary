using Microsoft.EntityFrameworkCore.Storage;
using Npgsql;

namespace FoodDiary.Persistence.PostgreSql;

/// <summary>Verifies PostgreSQL COMMIT so only a confirmed abort permits the caller to retry.</summary>
public static class VerifiedTransactionCommit {
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
            switch (status) {
                case "committed":
                    break;
                case "aborted":
                    throw;
                default:
                    // An unavailable or still-running outcome is never permission to replay the handler.
                    throw new CommitOutcomeUnknownException(commitException);
            }
        }
    }
}
