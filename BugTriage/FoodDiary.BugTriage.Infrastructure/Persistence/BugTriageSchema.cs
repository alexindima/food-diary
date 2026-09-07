using Npgsql;

namespace FoodDiary.BugTriage.Infrastructure.Persistence;

public static class BugTriageSchema {
    public static async Task InitializeAsync(NpgsqlDataSource dataSource, CancellationToken cancellationToken) {
        NpgsqlCommand command = dataSource.CreateCommand("""
            begin;
            select pg_advisory_xact_lock(724936128);
            create table if not exists bugtriage_reports (
                id uuid primary key,
                source_message_id uuid not null unique,
                received_at_utc timestamptz not null,
                subject text not null,
                text_body text not null,
                raw_mime bytea null,
                expires_at_utc timestamptz not null,
                status text not null default 'pending',
                attempt integer not null default 0,
                lease_token uuid null,
                lease_expires_at_utc timestamptz null,
                summary text null,
                merge_request_url text null,
                completed_token uuid null,
                constraint ck_bugtriage_status check (status in
                    ('pending','in_progress','needs_information','not_confirmed','duplicate','draft_ready','failed','expired','content_unavailable')),
                constraint ck_bugtriage_bounds check (char_length(subject) <= 1000 and char_length(text_body) <= 100000
                    and octet_length(raw_mime) <= 10485760 and char_length(summary) <= 8000 and char_length(merge_request_url) <= 2048)
            );
            create index if not exists ix_bugtriage_pending on bugtriage_reports (received_at_utc, id)
                where status in ('pending', 'in_progress');
            commit;
            """);
        await using System.Runtime.CompilerServices.ConfiguredAsyncDisposable commandScope = command.ConfigureAwait(false);
        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }
}
