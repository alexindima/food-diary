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
        NpgsqlConnection connection = await dataSource.OpenConnectionAsync(cancellationToken);
        await using (connection) {
            var command = new NpgsqlCommand(sql, connection);
            await using (command) {
                configure?.Invoke(command);

                NpgsqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);
                await using (reader) {
                    return await readAsync(reader, cancellationToken);
                }
            }
        }
    }

    public async Task<int> ExecuteAsync(
        string sql,
        Action<NpgsqlCommand>? configure,
        CancellationToken cancellationToken) {
        NpgsqlConnection connection = await dataSource.OpenConnectionAsync(cancellationToken);
        await using (connection) {
            var command = new NpgsqlCommand(sql, connection);
            await using (command) {
                configure?.Invoke(command);
                return await command.ExecuteNonQueryAsync(cancellationToken);
            }
        }
    }

    public async Task<T> ScalarAsync<T>(
        string sql,
        Action<NpgsqlCommand>? configure,
        Func<object, T> map,
        string missingValueError,
        CancellationToken cancellationToken) {
        NpgsqlConnection connection = await dataSource.OpenConnectionAsync(cancellationToken);
        await using (connection) {
            var command = new NpgsqlCommand(sql, connection);
            await using (command) {
                configure?.Invoke(command);

                object value = await command.ExecuteScalarAsync(cancellationToken)
                               ?? throw new InvalidOperationException(missingValueError);
                return map(value);
            }
        }
    }

    public async Task<T> InTransactionAsync<T>(
        Func<NpgsqlConnection, NpgsqlTransaction, CancellationToken, Task<T>> action,
        CancellationToken cancellationToken) {
        NpgsqlConnection connection = await dataSource.OpenConnectionAsync(cancellationToken);
        await using (connection) {
            NpgsqlTransaction transaction = await connection.BeginTransactionAsync(cancellationToken);
            await using (transaction) {
                T result = await action(connection, transaction, cancellationToken);
                await transaction.CommitAsync(cancellationToken);
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
        await using (command) {
            configure?.Invoke(command);

            NpgsqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);
            await using (reader) {
                return await readAsync(reader, cancellationToken);
            }
        }
    }
}
