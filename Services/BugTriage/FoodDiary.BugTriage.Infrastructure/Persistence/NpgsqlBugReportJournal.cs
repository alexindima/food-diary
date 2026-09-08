using FoodDiary.BugTriage.Application.Abstractions;
using FoodDiary.BugTriage.Application.Reports;
using Npgsql;
using NpgsqlTypes;

namespace FoodDiary.BugTriage.Infrastructure.Persistence;

public sealed class NpgsqlBugReportJournal(NpgsqlDataSource dataSource) : IBugReportJournal {
    public async Task<BugReportJournalPage> GetPageAsync(BugReportJournalFilter filter, DateTimeOffset now, CancellationToken cancellationToken) {
        const string predicate = """
            from bugtriage_reports
            where (@from is null or received_at_utc >= @from) and (@to is null or received_at_utc < @to)
                and (@id is null or id = @id)
                and (@status = '' or (case when expires_at_utc <= @now then 'expired' else status end) = @status)
                and (@search = '' or (expires_at_utc > @now and strpos(lower(subject), lower(@search)) > 0))
            """;
        const string projection = """
            select id, source_message_id, received_at_utc,
                case when expires_at_utc > @now then subject else '' end,
                case when expires_at_utc > @now then status else 'expired' end, attempt,
                case when expires_at_utc > @now then summary else null end,
                case when expires_at_utc > @now then merge_request_url else null end,
                expires_at_utc <= @now
            """;
        NpgsqlCommand command = dataSource.CreateCommand("select count(*) " + predicate + "; " + projection + " " + predicate + " order by received_at_utc desc, id desc limit @limit offset @offset");
        await using System.Runtime.CompilerServices.ConfiguredAsyncDisposable commandScope = command.ConfigureAwait(false);
        command.Parameters.AddWithValue("from", NpgsqlDbType.TimestampTz, (object?)filter.FromUtc?.ToUniversalTime() ?? DBNull.Value);
        command.Parameters.AddWithValue("to", NpgsqlDbType.TimestampTz, (object?)filter.ToUtc?.ToUniversalTime() ?? DBNull.Value);
        command.Parameters.AddWithValue("id", NpgsqlDbType.Uuid, (object?)filter.Id ?? DBNull.Value);
        command.Parameters.AddWithValue("now", now);
        command.Parameters.AddWithValue("status", filter.Status?.Trim() ?? "");
        command.Parameters.AddWithValue("search", filter.Search?.Trim() ?? "");
        command.Parameters.AddWithValue("limit", Math.Clamp(filter.Limit, 1, 100));
        command.Parameters.AddWithValue("offset", (Math.Clamp(filter.Page, 1, 10000) - 1) * Math.Clamp(filter.Limit, 1, 100));
        NpgsqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        await using System.Runtime.CompilerServices.ConfiguredAsyncDisposable readerScope = reader.ConfigureAwait(false);
        await reader.ReadAsync(cancellationToken).ConfigureAwait(false);
        long total = reader.GetInt64(0);
        await reader.NextResultAsync(cancellationToken).ConfigureAwait(false);
        var items = new List<BugReportJournalEntry>();
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false)) {
            items.Add(new BugReportJournalEntry(reader.GetGuid(0), reader.GetGuid(1),
                await reader.GetFieldValueAsync<DateTimeOffset>(2, cancellationToken).ConfigureAwait(false), reader.GetString(3), reader.GetString(4), reader.GetInt32(5),
                await reader.IsDBNullAsync(6, cancellationToken).ConfigureAwait(false) ? null : reader.GetString(6),
                await reader.IsDBNullAsync(7, cancellationToken).ConfigureAwait(false) ? null : reader.GetString(7), reader.GetBoolean(8)));
        }
        return new BugReportJournalPage(items, total);
    }
}
