namespace FoodDiary.MailRelay.Infrastructure.Services;

internal static class MailRelayQueueSchema {
    public const string EnsureSchemaSql = """
                                           create table if not exists mailrelay_outbound_emails (
                                               id uuid primary key,
                                               status text not null,
                                               from_address text not null,
                                               from_name text not null,
                                               to_recipients_json jsonb not null,
                                               subject text not null,
                                               html_body text not null,
                                               text_body text null,
                                               correlation_id text null,
                                               idempotency_key text null,
                                               attempt_count integer not null default 0,
                                               max_attempts integer not null,
                                               available_at_utc timestamptz not null,
                                               created_at_utc timestamptz not null,
                                               locked_at_utc timestamptz null,
                                               sent_at_utc timestamptz null,
                                               last_error text null
                                           );

                                           create unique index if not exists ux_mailrelay_outbound_emails_idempotency_key
                                               on mailrelay_outbound_emails (idempotency_key)
                                               where idempotency_key is not null;

                                           create index if not exists ix_mailrelay_outbound_emails_due
                                               on mailrelay_outbound_emails (status, available_at_utc, created_at_utc);

                                           create index if not exists ix_mailrelay_outbound_emails_processing
                                               on mailrelay_outbound_emails (status, locked_at_utc);

                                           create table if not exists mailrelay_outbox_messages (
                                               id uuid primary key,
                                               email_id uuid not null,
                                               status text not null,
                                               attempt_count integer not null default 0,
                                               available_at_utc timestamptz not null,
                                               locked_at_utc timestamptz null,
                                               published_at_utc timestamptz null,
                                               created_at_utc timestamptz not null,
                                               last_error text null
                                           );

                                           create index if not exists ix_mailrelay_outbox_messages_due
                                               on mailrelay_outbox_messages (status, available_at_utc, created_at_utc);

                                           create table if not exists mailrelay_inbox_messages (
                                               id uuid primary key,
                                               consumer_name text not null,
                                               message_key text not null,
                                               status text not null,
                                               locked_at_utc timestamptz not null,
                                               processed_at_utc timestamptz null,
                                               created_at_utc timestamptz not null,
                                               updated_at_utc timestamptz not null,
                                               last_error text null
                                           );

                                           create unique index if not exists ux_mailrelay_inbox_messages_consumer_message_key
                                               on mailrelay_inbox_messages (consumer_name, message_key);

                                           create table if not exists mailrelay_suppressions (
                                               email text primary key,
                                               reason text not null,
                                               source text not null,
                                               created_at_utc timestamptz not null,
                                               updated_at_utc timestamptz not null,
                                               expires_at_utc timestamptz null
                                           );

                                            create table if not exists mailrelay_delivery_events (
                                                id uuid primary key,
                                                event_type text not null,
                                               email text not null,
                                               source text not null,
                                               classification text null,
                                               provider_message_id text null,
                                               reason text null,
                                               occurred_at_utc timestamptz not null,
                                               created_at_utc timestamptz not null
                                           );

                                            create index if not exists ix_mailrelay_delivery_events_email_created
                                                on mailrelay_delivery_events (email, created_at_utc desc);

                                            create table if not exists mailrelay_schema_versions (
                                                version integer primary key,
                                                description text not null,
                                                applied_at_utc timestamptz not null
                                            );

                                            insert into mailrelay_schema_versions (
                                                version,
                                                description,
                                                applied_at_utc
                                            )
                                            values (
                                                1,
                                                'baseline mail relay queue, outbox, inbox, suppressions, and delivery events schema',
                                                now()
                                            )
                                            on conflict (version) do nothing;
                                           """;
}
