using Npgsql;

namespace FoodDiary.Infrastructure.Persistence.Locking;

internal sealed class PostgresAdvisoryLockLease : IAsyncDisposable {
    private static readonly TimeSpan RetryDelay = TimeSpan.FromMilliseconds(50);
    private readonly NpgsqlConnection _connection;
    private readonly NpgsqlCommand _unlockCommand;
    private int _disposed;

    private PostgresAdvisoryLockLease(NpgsqlConnection connection, NpgsqlCommand unlockCommand) {
        _connection = connection;
        _unlockCommand = unlockCommand;
    }

    public static Task<PostgresAdvisoryLockLease> AcquireAsync(
        string connectionString,
        long lockKey,
        CancellationToken cancellationToken) =>
        AcquireCoreAsync(
            connectionString,
            "SELECT pg_try_advisory_lock(@lock_key)",
            "SELECT pg_advisory_unlock(@lock_key)",
            command => command.Parameters.AddWithValue("lock_key", NpgsqlTypes.NpgsqlDbType.Bigint, lockKey),
            cancellationToken);

    public static Task<PostgresAdvisoryLockLease> AcquireAsync(
        string connectionString,
        string serializationKey,
        CancellationToken cancellationToken) =>
        AcquireCoreAsync(
            connectionString,
            "SELECT pg_try_advisory_lock(hashtextextended(@serialization_key, 0))",
            "SELECT pg_advisory_unlock(hashtextextended(@serialization_key, 0))",
            command => command.Parameters.AddWithValue("serialization_key", NpgsqlTypes.NpgsqlDbType.Text, serializationKey),
            cancellationToken);

    public async ValueTask DisposeAsync() {
        if (Interlocked.Exchange(ref _disposed, 1) != 0) {
            return;
        }

        try {
            _ = await _unlockCommand.ExecuteScalarAsync(CancellationToken.None).ConfigureAwait(false);
        } finally {
            await _unlockCommand.DisposeAsync().ConfigureAwait(false);
            await _connection.DisposeAsync().ConfigureAwait(false);
        }
    }

    private static async Task<PostgresAdvisoryLockLease> AcquireCoreAsync(
        string connectionString,
        string tryLockCommandText,
        string unlockCommandText,
        Action<NpgsqlCommand> addParameter,
        CancellationToken cancellationToken) {
        var connectionStringBuilder = new NpgsqlConnectionStringBuilder(connectionString) {
            Pooling = true,
            Multiplexing = false,
        };

        while (true) {
            var connection = new NpgsqlConnection(connectionStringBuilder.ConnectionString);
            bool leaseCreated = false;
            try {
                await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
                var tryLockCommand = new NpgsqlCommand(tryLockCommandText, connection);
                await using (tryLockCommand.ConfigureAwait(false)) {
                    addParameter(tryLockCommand);
                    bool acquired = (bool?)await tryLockCommand.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false) == true;
                    if (acquired) {
                        var unlockCommand = new NpgsqlCommand(unlockCommandText, connection);
                        addParameter(unlockCommand);
                        leaseCreated = true;
                        return new PostgresAdvisoryLockLease(connection, unlockCommand);
                    }
                }
            } finally {
                if (!leaseCreated) {
                    await connection.DisposeAsync().ConfigureAwait(false);
                }
            }

            await Task.Delay(RetryDelay, cancellationToken).ConfigureAwait(false);
        }
    }
}

