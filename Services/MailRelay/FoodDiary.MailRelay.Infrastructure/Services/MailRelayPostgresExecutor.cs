using System.Diagnostics.CodeAnalysis;
using Npgsql;

namespace FoodDiary.MailRelay.Infrastructure.Services;

[ExcludeFromCodeCoverage]
internal sealed class MailRelayPostgresExecutor(NpgsqlDataSource dataSource) {
    public async Task<T> QueryAsync<T>(
        string sql,
        Action<NpgsqlCommand>? configure,
        Func<NpgsqlDataReader, CancellationToken, Task<T>> readAsync,
        CancellationToken cancellationToken) {
        NpgsqlConnection connection = await dataSource.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        await using (connection.ConfigureAwait(false)) {
            var command = new NpgsqlCommand(sql, connection);
            await using (command.ConfigureAwait(false)) {
                configure?.Invoke(command);

                NpgsqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
                await using (reader.ConfigureAwait(false)) {
                    return await readAsync(reader, cancellationToken).ConfigureAwait(false);
                }
            }
        }
    }

    public async Task<int> ExecuteAsync(
        string sql,
        Action<NpgsqlCommand>? configure,
        CancellationToken cancellationToken) {
        NpgsqlConnection connection = await dataSource.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        await using (connection.ConfigureAwait(false)) {
            var command = new NpgsqlCommand(sql, connection);
            await using (command.ConfigureAwait(false)) {
                configure?.Invoke(command);
                return await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
            }
        }
    }

    public async Task<T> ScalarAsync<T>(
        string sql,
        Action<NpgsqlCommand>? configure,
        Func<object, T> map,
        string missingValueError,
        CancellationToken cancellationToken) {
        NpgsqlConnection connection = await dataSource.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        await using (connection.ConfigureAwait(false)) {
            var command = new NpgsqlCommand(sql, connection);
            await using (command.ConfigureAwait(false)) {
                configure?.Invoke(command);

                object value = await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false)
                               ?? throw new InvalidOperationException(missingValueError);
                return map(value);
            }
        }
    }

    public async Task<T> InTransactionAsync<T>(
        Func<NpgsqlConnection, NpgsqlTransaction, CancellationToken, Task<T>> action,
        CancellationToken cancellationToken) {
        NpgsqlConnection connection = await dataSource.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        await using (connection.ConfigureAwait(false)) {
            NpgsqlTransaction transaction = await connection.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
            await using (transaction.ConfigureAwait(false)) {
                T result = await action(connection, transaction, cancellationToken).ConfigureAwait(false);
                await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
                return result;
            }
        }
    }

    public async Task<T> QueryInTransactionAsync<T>(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        string sql,
        Action<NpgsqlCommand>? configure,
        Func<NpgsqlDataReader, CancellationToken, Task<T>> readAsync,
        CancellationToken cancellationToken) {
        var command = new NpgsqlCommand(sql, connection, transaction);
        await using (command.ConfigureAwait(false)) {
            configure?.Invoke(command);

            NpgsqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
            await using (reader.ConfigureAwait(false)) {
                return await readAsync(reader, cancellationToken).ConfigureAwait(false);
            }
        }
    }
}
