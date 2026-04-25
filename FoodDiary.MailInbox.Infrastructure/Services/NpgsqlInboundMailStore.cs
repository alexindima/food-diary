using System.Text.Json;
using FoodDiary.MailInbox.Application.Abstractions;
using FoodDiary.MailInbox.Application.Messages.Models;
using FoodDiary.MailInbox.Domain.Messages;
using Npgsql;

namespace FoodDiary.MailInbox.Infrastructure.Services;

public sealed class NpgsqlInboundMailStore(NpgsqlDataSource dataSource) : IInboundMailStore, IMailInboxSchemaInitializer {
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task EnsureSchemaAsync(CancellationToken cancellationToken) {
        const string sql = """
                           create table if not exists mailinbox_messages (
                               id uuid primary key,
                               message_id text null,
                               from_address text null,
                               to_recipients_json jsonb not null,
                               subject text null,
                               text_body text null,
                               html_body text null,
                               raw_mime text not null,
                               status text not null,
                               received_at_utc timestamptz not null
                           );

                           create index if not exists ix_mailinbox_messages_received_at_utc
                               on mailinbox_messages (received_at_utc desc);
                           """;

        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<Guid> SaveAsync(InboundMailMessage message, CancellationToken cancellationToken) {
        const string sql = """
                           insert into mailinbox_messages (
                               id,
                               message_id,
                               from_address,
                               to_recipients_json,
                               subject,
                               text_body,
                               html_body,
                               raw_mime,
                               status,
                               received_at_utc)
                           values (
                               @id,
                               @message_id,
                               @from_address,
                               @to_recipients_json::jsonb,
                               @subject,
                               @text_body,
                               @html_body,
                               @raw_mime,
                               @status,
                               @received_at_utc);
                           """;

        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("id", message.Id.Value);
        command.Parameters.AddWithNullableValue("message_id", message.MessageId);
        command.Parameters.AddWithNullableValue("from_address", message.FromAddress);
        command.Parameters.AddWithValue("to_recipients_json", JsonSerializer.Serialize(message.ToRecipients, JsonOptions));
        command.Parameters.AddWithNullableValue("subject", message.Subject);
        command.Parameters.AddWithNullableValue("text_body", message.TextBody);
        command.Parameters.AddWithNullableValue("html_body", message.HtmlBody);
        command.Parameters.AddWithValue("raw_mime", message.RawMime);
        command.Parameters.AddWithValue("status", message.Status.Value);
        command.Parameters.AddWithValue("received_at_utc", message.ReceivedAtUtc);
        await command.ExecuteNonQueryAsync(cancellationToken);
        return message.Id.Value;
    }

    public async Task<IReadOnlyList<InboundMailMessageSummary>> GetMessagesAsync(int limit, CancellationToken cancellationToken) {
        const string sql = """
                           select id, from_address, to_recipients_json::text, subject, status, received_at_utc
                           from mailinbox_messages
                           order by received_at_utc desc
                           limit @limit;
                           """;

        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("limit", limit);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        var messages = new List<InboundMailMessageSummary>();
        while (await reader.ReadAsync(cancellationToken)) {
            messages.Add(new InboundMailMessageSummary(
                reader.GetGuid(0),
                reader.GetNullableString(1),
                DeserializeRecipients(reader.GetString(2)),
                reader.GetNullableString(3),
                reader.GetString(4),
                reader.GetFieldValue<DateTimeOffset>(5)));
        }

        return messages;
    }

    public async Task<InboundMailMessageDetails?> GetMessageDetailsAsync(Guid id, CancellationToken cancellationToken) {
        const string sql = """
                           select id, message_id, from_address, to_recipients_json::text, subject, text_body, html_body, raw_mime, status, received_at_utc
                           from mailinbox_messages
                           where id = @id;
                           """;

        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        await using var command = new NpgsqlCommand(sql, connection);
        command.Parameters.AddWithValue("id", id);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken)) {
            return null;
        }

        return new InboundMailMessageDetails(
            reader.GetGuid(0),
            reader.GetNullableString(1),
            reader.GetNullableString(2),
            DeserializeRecipients(reader.GetString(3)),
            reader.GetNullableString(4),
            reader.GetNullableString(5),
            reader.GetNullableString(6),
            reader.GetString(7),
            reader.GetString(8),
            reader.GetFieldValue<DateTimeOffset>(9));
    }

    private static IReadOnlyList<string> DeserializeRecipients(string value) {
        return JsonSerializer.Deserialize<string[]>(value, JsonOptions) ?? [];
    }
}
