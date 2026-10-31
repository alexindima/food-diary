using System.Text.Json;
using FoodDiary.MailInbox.Application.Abstractions;
using FoodDiary.MailInbox.Application.Messages.Models;
using Npgsql;
using NpgsqlTypes;

namespace FoodDiary.MailInbox.Infrastructure.Services;

public sealed class NpgsqlInboundMailExportStore(NpgsqlDataSource dataSource) : IInboundMailExportStore {
    public async Task<IReadOnlyList<InboundMailExportEntry>> GetExportPageAsync(
        string recipient, DateTimeOffset? beforeReceivedAtUtc, Guid? beforeId, int limit,
        CancellationToken cancellationToken) {
        NpgsqlCommand command = dataSource.CreateCommand("""
            select id, received_at_utc, raw_mime is not null
            from mailinbox_messages
            where to_recipients_json @> @recipient
              and (@before_time::timestamptz is null or (received_at_utc, id) < (@before_time, @before_id))
            order by received_at_utc desc, id desc
            limit @limit
            """);
        await using IAsyncDisposable commandScope = command;
        command.Parameters.AddWithValue("recipient", NpgsqlDbType.Jsonb, JsonSerializer.Serialize(new[] { recipient.Trim().ToLowerInvariant() }));
        command.Parameters.AddWithValue("before_time", NpgsqlDbType.TimestampTz, (object?)beforeReceivedAtUtc?.ToUniversalTime() ?? DBNull.Value);
        command.Parameters.AddWithValue("before_id", NpgsqlDbType.Uuid, (object?)beforeId ?? DBNull.Value);
        command.Parameters.AddWithValue("limit", limit);
        NpgsqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);
        await using IAsyncDisposable readerScope = reader;
        var entries = new List<InboundMailExportEntry>();
        while (await reader.ReadAsync(cancellationToken)) {
            entries.Add(new InboundMailExportEntry(reader.GetGuid(0), await reader.GetFieldValueAsync<DateTimeOffset>(1, cancellationToken), reader.GetBoolean(2)));
        }
        return entries;
    }

    public async Task<byte[]?> GetRawMimeAsync(Guid id, string recipient, CancellationToken cancellationToken) {
        NpgsqlCommand command = dataSource.CreateCommand("""
            select raw_mime from mailinbox_messages
            where id = @id and to_recipients_json @> @recipient
            """);
        await using IAsyncDisposable commandScope = command;
        command.Parameters.AddWithValue("id", id);
        command.Parameters.AddWithValue("recipient", NpgsqlDbType.Jsonb, JsonSerializer.Serialize(new[] { recipient.Trim().ToLowerInvariant() }));
        return await command.ExecuteScalarAsync(cancellationToken) as byte[];
    }
}
