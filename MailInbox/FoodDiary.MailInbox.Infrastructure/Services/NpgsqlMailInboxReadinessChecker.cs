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
                                  and not exists (
                                      select 1
                                      from (values
                                          ('mailinbox_messages', 'id', 'uuid', 'NO'),
                                          ('mailinbox_messages', 'message_id', 'text', 'YES'),
                                          ('mailinbox_messages', 'from_address', 'text', 'YES'),
                                          ('mailinbox_messages', 'to_recipients_json', 'jsonb', 'NO'),
                                          ('mailinbox_messages', 'subject', 'text', 'YES'),
                                          ('mailinbox_messages', 'text_body', 'text', 'YES'),
                                          ('mailinbox_messages', 'html_body', 'text', 'YES'),
                                          ('mailinbox_messages', 'raw_mime', 'bytea', 'YES'),
                                          ('mailinbox_messages', 'status', 'text', 'NO'),
                                          ('mailinbox_messages', 'received_at_utc', 'timestamp with time zone', 'NO'),
                                          ('mailinbox_messages', 'read_at_utc', 'timestamp with time zone', 'YES'),
                                          ('mailinbox_messages', 'ingestion_key', 'text', 'NO'),
                                          ('mailinbox_messages', 'deduplication_bucket_utc', 'timestamp with time zone', 'NO'),
                                          ('mailinbox_messages', 'raw_size_bytes', 'bigint', 'NO'),
                                           ('mailinbox_messages', 'content_purged_at_utc', 'timestamp with time zone', 'YES'),
                                           ('mailinbox_messages', 'envelope_from_address', 'text', 'YES'),
                                           ('mailinbox_messages', 'is_trusted_relay', 'boolean', 'NO'),
                                          ('mailinbox_daily_ingestion_usage', 'usage_date', 'date', 'NO'),
                                          ('mailinbox_daily_ingestion_usage', 'message_count', 'bigint', 'NO'),
                                          ('mailinbox_daily_ingestion_usage', 'raw_bytes', 'bigint', 'NO'),
                                          ('mailinbox_daily_ingestion_usage', 'untrusted_message_count', 'bigint', 'NO'),
                                          ('mailinbox_daily_ingestion_usage', 'untrusted_raw_bytes', 'bigint', 'NO'),
                                          ('mailinbox_schema_migrations', 'name', 'text', 'NO'),
                                          ('mailinbox_schema_migrations', 'applied_at_utc', 'timestamp with time zone', 'NO')
                                      ) as expected(table_name, column_name, data_type, is_nullable)
                                      where not exists (
                                          select 1
                                          from information_schema.columns as actual
                                          where actual.table_schema = 'public'
                                            and actual.table_name = expected.table_name
                                            and actual.column_name = expected.column_name
                                            and actual.data_type = expected.data_type
                                            and actual.is_nullable = expected.is_nullable
                                      )
                                  )
                                  and exists (
                                      select 1
                                      from pg_constraint as constraint_definition
                                      where constraint_definition.conrelid = 'public.mailinbox_messages'::regclass
                                        and constraint_definition.contype = 'p'
                                        and constraint_definition.convalidated
                                        and (
                                            select array_agg(attribute.attname order by key.ordinality)
                                            from unnest(constraint_definition.conkey) with ordinality as key(attnum, ordinality)
                                            join pg_attribute as attribute on attribute.attrelid = constraint_definition.conrelid
                                                and attribute.attnum = key.attnum
                                        ) = array['id']::name[]
                                  )
                                  and exists (
                                      select 1
                                      from pg_constraint as constraint_definition
                                      where constraint_definition.conrelid = 'public.mailinbox_daily_ingestion_usage'::regclass
                                        and constraint_definition.contype = 'p'
                                        and constraint_definition.convalidated
                                        and (
                                            select array_agg(attribute.attname order by key.ordinality)
                                            from unnest(constraint_definition.conkey) with ordinality as key(attnum, ordinality)
                                            join pg_attribute as attribute on attribute.attrelid = constraint_definition.conrelid
                                                and attribute.attnum = key.attnum
                                        ) = array['usage_date']::name[]
                                  )
                                  and exists (
                                      select 1
                                      from pg_constraint as constraint_definition
                                      where constraint_definition.conrelid = 'public.mailinbox_schema_migrations'::regclass
                                        and constraint_definition.contype = 'p'
                                        and constraint_definition.convalidated
                                        and (
                                            select array_agg(attribute.attname order by key.ordinality)
                                            from unnest(constraint_definition.conkey) with ordinality as key(attnum, ordinality)
                                            join pg_attribute as attribute on attribute.attrelid = constraint_definition.conrelid
                                                and attribute.attnum = key.attnum
                                        ) = array['name']::name[]
                                  )
                                  and exists (
                                      select 1
                                      from pg_index as index_definition
                                      where index_definition.indexrelid = to_regclass('public.ix_mailinbox_messages_received_at_utc')
                                        and index_definition.indrelid = 'public.mailinbox_messages'::regclass
                                        and index_definition.indisvalid
                                        and index_definition.indisready
                                        and index_definition.indpred is null
                                        and index_definition.indexprs is null
                                        and (
                                            select array_agg(attribute.attname order by key.ordinality)
                                            from unnest(index_definition.indkey) with ordinality as key(attnum, ordinality)
                                            join pg_attribute as attribute on attribute.attrelid = index_definition.indrelid
                                                and attribute.attnum = key.attnum
                                        ) = array['received_at_utc']::name[]
                                  )
                                  and exists (
                                      select 1
                                      from pg_index as index_definition
                                      where index_definition.indexrelid = to_regclass('public.ix_mailinbox_messages_unread_received_at_utc')
                                        and index_definition.indrelid = 'public.mailinbox_messages'::regclass
                                        and index_definition.indisvalid
                                        and index_definition.indisready
                                        and lower(pg_get_expr(index_definition.indpred, index_definition.indrelid)) like '%read_at_utc%is null%'
                                        and index_definition.indexprs is null
                                        and (
                                            select array_agg(attribute.attname order by key.ordinality)
                                            from unnest(index_definition.indkey) with ordinality as key(attnum, ordinality)
                                            join pg_attribute as attribute on attribute.attrelid = index_definition.indrelid
                                                and attribute.attnum = key.attnum
                                        ) = array['received_at_utc']::name[]
                                  )
                                  and exists (
                                      select 1
                                      from pg_index as index_definition
                                      where index_definition.indexrelid = to_regclass('public.ux_mailinbox_messages_ingestion_window')
                                        and index_definition.indrelid = 'public.mailinbox_messages'::regclass
                                        and index_definition.indisunique
                                        and index_definition.indisvalid
                                        and index_definition.indisready
                                        and index_definition.indpred is null
                                        and index_definition.indexprs is null
                                        and (
                                            select array_agg(attribute.attname order by key.ordinality)
                                            from unnest(index_definition.indkey) with ordinality as key(attnum, ordinality)
                                            join pg_attribute as attribute on attribute.attrelid = index_definition.indrelid
                                                and attribute.attnum = key.attnum
                                        ) = array['ingestion_key', 'deduplication_bucket_utc']::name[]
                                  )
                                  and exists (
                                      select 1
                                      from pg_index as index_definition
                                      where index_definition.indexrelid = to_regclass('public.ix_mailinbox_messages_content_retention')
                                        and index_definition.indrelid = 'public.mailinbox_messages'::regclass
                                        and index_definition.indisvalid
                                        and index_definition.indisready
                                        and lower(pg_get_expr(index_definition.indpred, index_definition.indrelid)) like '%content_purged_at_utc%is null%'
                                        and index_definition.indexprs is null
                                        and (
                                            select array_agg(attribute.attname order by key.ordinality)
                                            from unnest(index_definition.indkey) with ordinality as key(attnum, ordinality)
                                            join pg_attribute as attribute on attribute.attrelid = index_definition.indrelid
                                                and attribute.attnum = key.attnum
                                        ) = array['received_at_utc']::name[]
                                  )
                                  and exists (
                                      select 1
                                      from pg_index as index_definition
                                      where index_definition.indexrelid = to_regclass('public.ix_mailinbox_messages_ingestion_received_at_utc')
                                        and index_definition.indrelid = 'public.mailinbox_messages'::regclass
                                        and index_definition.indisvalid
                                        and index_definition.indisready
                                        and index_definition.indpred is null
                                        and index_definition.indexprs is null
                                        and (
                                            select array_agg(attribute.attname order by key.ordinality)
                                            from unnest(index_definition.indkey) with ordinality as key(attnum, ordinality)
                                            join pg_attribute as attribute on attribute.attrelid = index_definition.indrelid
                                                and attribute.attnum = key.attnum
                                        ) = array['ingestion_key', 'received_at_utc']::name[]
                                  )
                                  and (
                                      select count(*)
                                      from mailinbox_schema_migrations
                                      where name = any(array[
                                          '202606140001_create_mailinbox_messages',
                                          '202606140002_add_mailinbox_message_read_at_utc',
                                          '202608170001_harden_mailinbox_ingestion',
                                          '202608190001_add_sliding_dedup_index',
                                           '202608190002_bound_persisted_mail_metadata',
                                           '202608200001_reserve_trusted_ingestion_capacity',
                                           '202608200002_add_mail_authentication_provenance'
                                       ])
                                   ) = 7
                                  and exists (
                                      select 1
                                      from pg_proc as function_definition
                                      where function_definition.oid = to_regprocedure('public.mailinbox_recipients_within_limits(jsonb)')
                                        and function_definition.prorettype = 'boolean'::regtype
                                        and function_definition.provolatile = 'i'
                                        and function_definition.proisstrict
                                        and lower(function_definition.prosrc) like '%jsonb_array_length%between 1 and 100%'
                                        and lower(function_definition.prosrc) like '%char_length%> 320%'
                                  )
                                   and exists (
                                       select 1
                                       from pg_constraint as constraint_definition
                                      where constraint_definition.conrelid = 'public.mailinbox_messages'::regclass
                                        and constraint_definition.contype = 'c'
                                        and constraint_definition.conname = 'ck_mailinbox_messages_message_id_length'
                                        and constraint_definition.convalidated
                                        and lower(pg_get_constraintdef(constraint_definition.oid)) like '%char_length(message_id)%<= 998%'
                                  )
                                  and exists (
                                      select 1
                                      from pg_constraint as constraint_definition
                                      where constraint_definition.conrelid = 'public.mailinbox_messages'::regclass
                                        and constraint_definition.contype = 'c'
                                        and constraint_definition.conname = 'ck_mailinbox_messages_from_address_length'
                                        and constraint_definition.convalidated
                                        and lower(pg_get_constraintdef(constraint_definition.oid)) like '%char_length(from_address)%<= 320%'
                                  )
                                  and exists (
                                      select 1
                                      from pg_constraint as constraint_definition
                                      where constraint_definition.conrelid = 'public.mailinbox_messages'::regclass
                                        and constraint_definition.contype = 'c'
                                        and constraint_definition.conname = 'ck_mailinbox_messages_subject_length'
                                        and constraint_definition.convalidated
                                        and lower(pg_get_constraintdef(constraint_definition.oid)) like '%char_length(subject)%<= 998%'
                                  )
                                  and exists (
                                      select 1
                                      from pg_constraint as constraint_definition
                                      where constraint_definition.conrelid = 'public.mailinbox_messages'::regclass
                                        and constraint_definition.contype = 'c'
                                        and constraint_definition.conname = 'ck_mailinbox_messages_recipients_limits'
                                        and constraint_definition.convalidated
                                         and lower(pg_get_constraintdef(constraint_definition.oid)) like '%mailinbox_recipients_within_limits(to_recipients_json)%'
                                   )
                                   and exists (
                                       select 1
                                       from pg_constraint as constraint_definition
                                       where constraint_definition.conrelid = 'public.mailinbox_messages'::regclass
                                         and constraint_definition.contype = 'c'
                                         and constraint_definition.conname = 'ck_mailinbox_messages_envelope_from_address_length'
                                         and constraint_definition.convalidated
                                         and lower(pg_get_constraintdef(constraint_definition.oid)) like '%char_length(envelope_from_address)%<= 320%'
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
