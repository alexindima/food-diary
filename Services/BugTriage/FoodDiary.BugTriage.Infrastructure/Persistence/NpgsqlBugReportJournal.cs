using FoodDiary.BugTriage.Application.Abstractions;
using FoodDiary.BugTriage.Application.Reports;
using Npgsql;
using NpgsqlTypes;

namespace FoodDiary.BugTriage.Infrastructure.Persistence;

public sealed class NpgsqlBugReportJournal(NpgsqlDataSource dataSource, IAuditRunStore auditRuns) : IBugReportJournal {
    public async Task<BugReportJournalPage> GetPageAsync(BugReportJournalFilter filter, DateTimeOffset now, CancellationToken cancellationToken) {
        const string rows = """
            with reports as (
                select id, source_message_id, received_at_utc, subject, status, attempt, summary, merge_request_url,
                    expires_at_utc, 'email'::text as source, null::uuid as audit_run_id,
                    null::timestamptz as audit_from_utc, null::timestamptz as audit_to_utc, report_title, short_summary
                from bugtriage_reports
                union all
                select f.id, null::uuid, a.started_at_utc, f.title, f.status, 1, f.summary, f.merge_request_url,
                    a.expires_at_utc, 'logs'::text, a.id, a.from_utc, a.to_utc, null::text, null::text
                from bugtriage_audit_findings f join bugtriage_audit_runs a on a.id = f.audit_run_id
            )
            """;
        const string predicate = """
            from reports
            where (@from is null or received_at_utc >= @from) and (@to is null or received_at_utc < @to)
                and (@id is null or id = @id)
                and (@source = '' or source = @source)
                and (@status = '' or (case when expires_at_utc <= @now then 'expired' else status end) = @status)
                and (@search = '' or (expires_at_utc > @now and
                    (strpos(lower(subject), lower(@search)) > 0 or strpos(lower(report_title), lower(@search)) > 0)))
            """;
        const string projection = """
            select id, source_message_id, received_at_utc,
                case when expires_at_utc > @now then subject else '' end,
                case when expires_at_utc > @now then status else 'expired' end, attempt,
                case when expires_at_utc > @now then summary else null end,
                case when expires_at_utc > @now then merge_request_url else null end,
                expires_at_utc <= @now, source, audit_run_id, audit_from_utc, audit_to_utc,
                case when expires_at_utc > @now then report_title else null end,
                case when expires_at_utc > @now then short_summary else null end
            """;
        NpgsqlCommand command = dataSource.CreateCommand(rows + " select count(*) " + predicate + "; " + rows + " " + projection + " " + predicate + " order by received_at_utc desc, id desc limit @limit offset @offset");
        await using System.Runtime.CompilerServices.ConfiguredAsyncDisposable commandScope = command.ConfigureAwait(false);
        command.Parameters.AddWithValue("from", NpgsqlDbType.TimestampTz, (object?)filter.FromUtc?.ToUniversalTime() ?? DBNull.Value);
        command.Parameters.AddWithValue("to", NpgsqlDbType.TimestampTz, (object?)filter.ToUtc?.ToUniversalTime() ?? DBNull.Value);
        command.Parameters.AddWithValue("id", NpgsqlDbType.Uuid, (object?)filter.Id ?? DBNull.Value);
        command.Parameters.AddWithValue("now", now);
        command.Parameters.AddWithValue("status", filter.Status?.Trim() ?? "");
        command.Parameters.AddWithValue("search", filter.Search?.Trim() ?? "");
        command.Parameters.AddWithValue("source", filter.Source ?? "");
        command.Parameters.AddWithValue("limit", Math.Clamp(filter.Limit, 1, 100));
        command.Parameters.AddWithValue("offset", (Math.Clamp(filter.Page, 1, 10000) - 1) * Math.Clamp(filter.Limit, 1, 100));
        NpgsqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        await using System.Runtime.CompilerServices.ConfiguredAsyncDisposable readerScope = reader.ConfigureAwait(false);
        await reader.ReadAsync(cancellationToken).ConfigureAwait(false);
        long total = reader.GetInt64(0);
        await reader.NextResultAsync(cancellationToken).ConfigureAwait(false);
        var items = new List<BugReportJournalEntry>();
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false)) {
            items.Add(new BugReportJournalEntry(reader.GetGuid(0), await reader.IsDBNullAsync(1, cancellationToken).ConfigureAwait(false) ? null : reader.GetGuid(1),
                await reader.GetFieldValueAsync<DateTimeOffset>(2, cancellationToken).ConfigureAwait(false), reader.GetString(3), reader.GetString(4), reader.GetInt32(5),
                await reader.IsDBNullAsync(6, cancellationToken).ConfigureAwait(false) ? null : reader.GetString(6),
                await reader.IsDBNullAsync(7, cancellationToken).ConfigureAwait(false) ? null : reader.GetString(7), reader.GetBoolean(8)) {
                Source = reader.GetString(9),
                Title = await reader.IsDBNullAsync(13, cancellationToken).ConfigureAwait(false) ? null : reader.GetString(13),
                ShortSummary = await reader.IsDBNullAsync(14, cancellationToken).ConfigureAwait(false) ? null : reader.GetString(14),
                AuditRunId = await reader.IsDBNullAsync(10, cancellationToken).ConfigureAwait(false) ? null : reader.GetGuid(10),
                AuditFromUtc = await reader.IsDBNullAsync(11, cancellationToken).ConfigureAwait(false) ? null :
                    await reader.GetFieldValueAsync<DateTimeOffset>(11, cancellationToken).ConfigureAwait(false),
                AuditToUtc = await reader.IsDBNullAsync(12, cancellationToken).ConfigureAwait(false) ? null :
                    await reader.GetFieldValueAsync<DateTimeOffset>(12, cancellationToken).ConfigureAwait(false),
            });
        }
        await reader.CloseAsync().ConfigureAwait(false);
        return new BugReportJournalPage(items, total) { LatestAudit = await auditRuns.GetLatestAsync(now, cancellationToken).ConfigureAwait(false) };
    }
}
