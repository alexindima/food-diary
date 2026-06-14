using System.Text.Json;
using FoodDiary.MailInbox.Application.Abstractions;
using FoodDiary.MailInbox.Application.Messages.Models;
using FoodDiary.MailInbox.Domain.Messages;
using Npgsql;

namespace FoodDiary.MailInbox.Infrastructure.Services;

public sealed class NpgsqlInboundMailStore(
    NpgsqlDataSource dataSource,
    DmarcReportParser dmarcReportParser) : IInboundMailStore, IMailInboxSchemaInitializer {
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private static readonly MailInboxSchemaMigration[] SchemaMigrations = [
        new(
            "202606140001_create_mailinbox_messages",
            """
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
            """),
        new(
            "202606140002_add_mailinbox_message_read_at_utc",
            """
            alter table mailinbox_messages
                add column if not exists read_at_utc timestamptz null;

            create index if not exists ix_mailinbox_messages_unread_received_at_utc
                on mailinbox_messages (received_at_utc desc)
                where read_at_utc is null;
            """),
    ];

    public async Task EnsureSchemaAsync(CancellationToken cancellationToken) {
        NpgsqlConnection connection = await dataSource.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        await using (connection.ConfigureAwait(false)) {
            await EnsureMigrationTableAsync(connection, cancellationToken).ConfigureAwait(false);

            foreach (MailInboxSchemaMigration migration in SchemaMigrations) {
                if (await IsMigrationAppliedAsync(connection, migration.Name, cancellationToken).ConfigureAwait(false)) {
                    continue;
                }

                await ApplyMigrationAsync(connection, migration, cancellationToken).ConfigureAwait(false);
            }
        }
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

        NpgsqlConnection connection = await dataSource.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        await using (connection.ConfigureAwait(false)) {
            var command = new NpgsqlCommand(sql, connection);
            await using (command.ConfigureAwait(false)) {
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
                await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
                return message.Id.Value;
            }
        }
    }

    public async Task<IReadOnlyList<InboundMailMessageSummary>> GetMessagesAsync(int limit, CancellationToken cancellationToken) {
        const string sql = """
                           select id, from_address, to_recipients_json::text, subject, status, read_at_utc, received_at_utc
                           from mailinbox_messages
                           order by received_at_utc desc
                           limit @limit;
                           """;

        NpgsqlConnection connection = await dataSource.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        await using (connection.ConfigureAwait(false)) {
            var command = new NpgsqlCommand(sql, connection);
            await using (command.ConfigureAwait(false)) {
                command.Parameters.AddWithValue("limit", limit);
                NpgsqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
                await using (reader.ConfigureAwait(false)) {
                    var messages = new List<InboundMailMessageSummary>();
                    while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false)) {
                        IReadOnlyList<string> recipients = DeserializeRecipients(reader.GetString(2));
                        string? subject = reader.GetNullableString(3);
                        messages.Add(new InboundMailMessageSummary(
                            reader.GetGuid(0),
                            reader.GetNullableString(1),
                            recipients,
                            subject,
                            GetCategory(recipients, subject),
                            reader.GetString(4),
                            await reader.GetNullableDateTimeOffsetAsync(5, cancellationToken).ConfigureAwait(false),
                            await reader.GetFieldValueAsync<DateTimeOffset>(6, cancellationToken).ConfigureAwait(false)));
                    }

                    return messages;
                }
            }
        }
    }

    public async Task<InboundMailMessageDetails?> GetMessageDetailsAsync(Guid id, CancellationToken cancellationToken) {
        const string sql = """
                           select id, message_id, from_address, to_recipients_json::text, subject, text_body, html_body, raw_mime, status, read_at_utc, received_at_utc
                           from mailinbox_messages
                           where id = @id;
                           """;

        NpgsqlConnection connection = await dataSource.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        await using (connection.ConfigureAwait(false)) {
            var command = new NpgsqlCommand(sql, connection);
            await using (command.ConfigureAwait(false)) {
                command.Parameters.AddWithValue("id", id);
                NpgsqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
                await using (reader.ConfigureAwait(false)) {
                    if (!await reader.ReadAsync(cancellationToken).ConfigureAwait(false)) {
                        return null;
                    }

                    IReadOnlyList<string> recipients = DeserializeRecipients(reader.GetString(3));
                    string? subject = reader.GetNullableString(4);
                    string rawMime = reader.GetString(7);
                    DmarcReportPreview? dmarcReport = dmarcReportParser.TryParse(rawMime);

                    return new InboundMailMessageDetails(
                        reader.GetGuid(0),
                        reader.GetNullableString(1),
                        reader.GetNullableString(2),
                        recipients,
                        subject,
                        reader.GetNullableString(5),
                        reader.GetNullableString(6),
                        rawMime,
                        dmarcReport is null ? GetCategory(recipients, subject) : InboundMailMessageCategories.DmarcReport,
                        dmarcReport,
                        reader.GetString(8),
                        await reader.GetNullableDateTimeOffsetAsync(9, cancellationToken).ConfigureAwait(false),
                        await reader.GetFieldValueAsync<DateTimeOffset>(10, cancellationToken).ConfigureAwait(false));
                }
            }
        }
    }

    public async Task<bool> MarkAsReadAsync(Guid id, DateTimeOffset readAtUtc, CancellationToken cancellationToken) {
        const string sql = """
                           update mailinbox_messages
                           set read_at_utc = coalesce(read_at_utc, @read_at_utc)
                           where id = @id;
                           """;

        NpgsqlConnection connection = await dataSource.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        await using (connection.ConfigureAwait(false)) {
            var command = new NpgsqlCommand(sql, connection);
            await using (command.ConfigureAwait(false)) {
                command.Parameters.AddWithValue("id", id);
                command.Parameters.AddWithValue("read_at_utc", readAtUtc.ToUniversalTime());
                return await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false) > 0;
            }
        }
    }

    private static IReadOnlyList<string> DeserializeRecipients(string value) {
        return JsonSerializer.Deserialize<string[]>(value, JsonOptions) ?? [];
    }

    private static string GetCategory(IReadOnlyList<string> recipients, string? subject) {
        if (recipients.Any(static recipient => recipient.Equals("dmarc@fooddiary.club", StringComparison.OrdinalIgnoreCase)) ||
            subject?.Contains("DMARC", StringComparison.OrdinalIgnoreCase) == true ||
            subject?.Contains("Report Domain:", StringComparison.OrdinalIgnoreCase) == true) {
            return InboundMailMessageCategories.DmarcReport;
        }

        return InboundMailMessageCategories.General;
    }

    private static async Task EnsureMigrationTableAsync(NpgsqlConnection connection, CancellationToken cancellationToken) {
        const string sql = """
                           create table if not exists mailinbox_schema_migrations (
                               name text primary key,
                               applied_at_utc timestamptz not null
                           );
                           """;

        var command = new NpgsqlCommand(sql, connection);
        await using (command.ConfigureAwait(false)) {
            await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    private static async Task<bool> IsMigrationAppliedAsync(
        NpgsqlConnection connection,
        string name,
        CancellationToken cancellationToken) {
        const string sql = """
                           select exists (
                               select 1
                               from mailinbox_schema_migrations
                               where name = @name
                           );
                           """;

        var command = new NpgsqlCommand(sql, connection);
        await using (command.ConfigureAwait(false)) {
            command.Parameters.AddWithValue("name", name);
            object? result = await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
            return result is true;
        }
    }

    private static async Task ApplyMigrationAsync(
        NpgsqlConnection connection,
        MailInboxSchemaMigration migration,
        CancellationToken cancellationToken) {
        NpgsqlTransaction transaction = await connection.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
        await using (transaction.ConfigureAwait(false)) {
            var migrationCommand = new NpgsqlCommand(migration.Sql, connection, transaction);
            await using (migrationCommand.ConfigureAwait(false)) {
                await migrationCommand.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
            }

            const string insertSql = """
                                     insert into mailinbox_schema_migrations (name, applied_at_utc)
                                     values (@name, @applied_at_utc)
                                     on conflict (name) do nothing;
                                     """;
            var insertCommand = new NpgsqlCommand(insertSql, connection, transaction);
            await using (insertCommand.ConfigureAwait(false)) {
                insertCommand.Parameters.AddWithValue("name", migration.Name);
                insertCommand.Parameters.AddWithValue("applied_at_utc", DateTimeOffset.UtcNow);
                await insertCommand.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
            }

            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    private sealed record MailInboxSchemaMigration(string Name, string Sql);
}
