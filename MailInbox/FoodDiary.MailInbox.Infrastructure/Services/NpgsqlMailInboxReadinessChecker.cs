using FoodDiary.MailInbox.Application.Abstractions;
using Npgsql;

namespace FoodDiary.MailInbox.Infrastructure.Services;

public sealed class NpgsqlMailInboxReadinessChecker(NpgsqlDataSource dataSource) : IMailInboxReadinessChecker {
    public async Task CheckReadyAsync(CancellationToken cancellationToken) {
        NpgsqlConnection connection = await dataSource.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        await using (connection.ConfigureAwait(false)) {
            const string sql = """
                               select to_regclass('public.mailinbox_messages') is not null
                                  and to_regclass('public.mailinbox_schema_migrations') is not null
                                  and to_regclass('public.mailinbox_daily_ingestion_usage') is not null
                                  and exists (
                                      select 1
                                      from information_schema.columns
                                      where table_schema = 'public'
                                        and table_name = 'mailinbox_messages'
                                        and column_name = 'read_at_utc'
                                        and data_type = 'timestamp with time zone'
                                  )
                                  and exists (
                                      select 1
                                      from information_schema.columns
                                      where table_schema = 'public'
                                        and table_name = 'mailinbox_messages'
                                        and column_name = 'ingestion_key'
                                        and data_type = 'text'
                                        and is_nullable = 'NO'
                                  )
                                  and exists (
                                      select 1
                                      from information_schema.columns
                                      where table_schema = 'public'
                                        and table_name = 'mailinbox_messages'
                                        and column_name = 'deduplication_bucket_utc'
                                        and data_type = 'timestamp with time zone'
                                        and is_nullable = 'NO'
                                  )
                                  and exists (
                                      select 1
                                      from information_schema.columns
                                      where table_schema = 'public'
                                        and table_name = 'mailinbox_messages'
                                        and column_name = 'raw_mime'
                                        and data_type = 'bytea'
                                  )
                                  and exists (
                                      select 1
                                      from information_schema.columns
                                      where table_schema = 'public'
                                        and table_name = 'mailinbox_messages'
                                        and column_name = 'raw_size_bytes'
                                        and data_type = 'bigint'
                                        and is_nullable = 'NO'
                                  )
                                  and exists (
                                      select 1
                                      from information_schema.columns
                                      where table_schema = 'public'
                                        and table_name = 'mailinbox_messages'
                                        and column_name = 'content_purged_at_utc'
                                        and data_type = 'timestamp with time zone'
                                  )
                                  and exists (
                                      select 1
                                      from pg_index
                                      where indexrelid = to_regclass('public.ux_mailinbox_messages_ingestion_window')
                                        and indisunique
                                        and indisvalid
                                  )
                                  and exists (
                                      select 1
                                      from pg_index
                                      where indexrelid = to_regclass('public.ix_mailinbox_messages_ingestion_received_at_utc')
                                        and indisvalid
                                  )
                                  and exists (
                                      select 1
                                      from mailinbox_schema_migrations
                                      where name = '202608190002_bound_persisted_mail_metadata'
                                  )
                                  and exists (
                                      select 1
                                      from pg_constraint
                                      where conrelid = 'public.mailinbox_messages'::regclass
                                        and conname = 'ck_mailinbox_messages_message_id_length'
                                        and convalidated
                                  )
                                  and exists (
                                      select 1
                                      from pg_constraint
                                      where conrelid = 'public.mailinbox_messages'::regclass
                                        and conname = 'ck_mailinbox_messages_from_address_length'
                                        and convalidated
                                  )
                                  and exists (
                                      select 1
                                      from pg_constraint
                                      where conrelid = 'public.mailinbox_messages'::regclass
                                        and conname = 'ck_mailinbox_messages_subject_length'
                                        and convalidated
                                  )
                                  and exists (
                                      select 1
                                      from pg_constraint
                                      where conrelid = 'public.mailinbox_messages'::regclass
                                        and conname = 'ck_mailinbox_messages_recipients_limits'
                                        and convalidated
                                  );
                               """;
            try {
                var command = new NpgsqlCommand(sql, connection);
                await using (command.ConfigureAwait(false)) {
                    object? result = await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
                    if (result is not true) {
                        throw new InvalidOperationException("MailInbox schema is not ready: required schema objects are missing or invalid.");
                    }
                }
            } catch (PostgresException exception) when (string.Equals(
                exception.SqlState,
                PostgresErrorCodes.UndefinedTable,
                StringComparison.Ordinal)) {
                throw new InvalidOperationException(
                    "MailInbox schema is not ready: required schema objects are missing or invalid.",
                    exception);
            }
        }
    }
}
