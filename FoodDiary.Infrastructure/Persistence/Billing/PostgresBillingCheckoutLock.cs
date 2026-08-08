using System.Data;
using FoodDiary.Application.Abstractions.Billing.Common;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace FoodDiary.Infrastructure.Persistence.Billing;

public sealed class PostgresBillingCheckoutLock(FoodDiaryDbContext context) : IBillingCheckoutLock {
    public async Task<IAsyncDisposable> AcquireAsync(Guid userId, CancellationToken cancellationToken = default) {
        long lockKey = BitConverter.ToInt64(userId.ToByteArray(), startIndex: 0);
        await context.Database.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        try {
            var connection = (NpgsqlConnection)context.Database.GetDbConnection();
            var command = new NpgsqlCommand("SELECT pg_advisory_lock(@lock_key)", connection);
            await using (command.ConfigureAwait(false)) {
                command.Parameters.AddWithValue("lock_key", NpgsqlTypes.NpgsqlDbType.Bigint, lockKey);
                await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
            }
            return new Releaser(context, connection, lockKey);
        } catch {
            await context.Database.CloseConnectionAsync().ConfigureAwait(false);
            throw;
        }
    }

    private sealed class Releaser(FoodDiaryDbContext context, NpgsqlConnection connection, long lockKey) : IAsyncDisposable {
        public async ValueTask DisposeAsync() {
            if (connection.State == ConnectionState.Open) {
                var command = new NpgsqlCommand("SELECT pg_advisory_unlock(@lock_key)", connection);
                await using (command.ConfigureAwait(false)) {
                    command.Parameters.AddWithValue("lock_key", NpgsqlTypes.NpgsqlDbType.Bigint, lockKey);
                    await command.ExecuteNonQueryAsync().ConfigureAwait(false);
                }
            }

            await context.Database.CloseConnectionAsync().ConfigureAwait(false);
        }
    }
}
