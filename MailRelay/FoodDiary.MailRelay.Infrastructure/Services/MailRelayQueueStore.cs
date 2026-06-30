using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Options;
using Npgsql;

namespace FoodDiary.MailRelay.Infrastructure.Services;

public sealed partial class MailRelayQueueStore(
    NpgsqlDataSource dataSource,
    IOptions<MailRelayQueueOptions> queueOptions,
    TimeProvider timeProvider) : IMailRelayQueueStore, IMailRelaySchemaInitializer {
    private static readonly System.Text.Json.JsonSerializerOptions JsonOptions = new(System.Text.Json.JsonSerializerDefaults.Web);
    private readonly NpgsqlDataSource _dataSource = dataSource;
    private readonly MailRelayPostgresExecutor _executor = new(dataSource);
    private readonly MailRelayQueueOptions _queueOptions = queueOptions.Value;

    public async Task EnsureSchemaAsync(CancellationToken cancellationToken) {
        NpgsqlConnection connection = await _dataSource.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        await using (connection.ConfigureAwait(false)) {
            var command = new NpgsqlCommand(MailRelayQueueSchema.EnsureSchemaSql, connection);
            await using (command.ConfigureAwait(false)) {
                await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
            }
        }
    }

    private async Task ExecuteStatusCommandAsync(string sql, Guid id, CancellationToken cancellationToken) {
        NpgsqlConnection connection = await _dataSource.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        await using (connection.ConfigureAwait(false)) {
            var command = new NpgsqlCommand(sql, connection);
            await using (command.ConfigureAwait(false)) {
                command.Parameters.AddWithValue("id", id);
                await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
            }
        }
    }

    private TimeSpan ComputeBackoff(int attemptCount) {
        int exponent = Math.Max(0, attemptCount - 1);
        double retrySeconds = _queueOptions.BaseRetryDelaySeconds * Math.Pow(2, exponent);
        double boundedSeconds = Math.Min(retrySeconds, _queueOptions.MaxRetryDelaySeconds);
        return TimeSpan.FromSeconds(boundedSeconds);
    }

    private static string Truncate(string value, int maxLength) {
        return value.Length <= maxLength ? value : value[..maxLength];
    }

    [ExcludeFromCodeCoverage]
    private static async Task RequireReturnedRowAsync(
        NpgsqlDataReader reader,
        CancellationToken cancellationToken,
        string missingRowMessage) {
        if (!await reader.ReadAsync(cancellationToken).ConfigureAwait(false)) {
            throw new InvalidOperationException(missingRowMessage);
        }
    }

    private sealed record InsertQueuedEmailResult(Guid Id, bool Inserted);
}
