using FoodDiary.BugTriage.Application.Abstractions;
using FoodDiary.BugTriage.Application.Reports;
using Npgsql;
using NpgsqlTypes;
using FoodDiary.BugTriage.Infrastructure.Options;
using Microsoft.Extensions.Options;

namespace FoodDiary.BugTriage.Infrastructure.Persistence;

public sealed class NpgsqlBugReportStore(NpgsqlDataSource dataSource, IOptions<BugTriageOptions>? options = null) : IBugReportStore {
    public async Task<IReadOnlyList<ReportSummary>> GetRecentAsync(DateTimeOffset now, CancellationToken cancellationToken) {
        NpgsqlCommand command = dataSource.CreateCommand("""
            select id, status, attempt, summary, merge_request_url from bugtriage_reports
            where expires_at_utc > @now order by received_at_utc desc, id desc limit 50
            """);
        await using System.Runtime.CompilerServices.ConfiguredAsyncDisposable commandScope = command.ConfigureAwait(false);
        command.Parameters.AddWithValue("now", now);
        NpgsqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        await using System.Runtime.CompilerServices.ConfiguredAsyncDisposable readerScope = reader.ConfigureAwait(false);
        var reports = new List<ReportSummary>();
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false)) {
            reports.Add(new ReportSummary(reader.GetGuid(0), reader.GetString(1), reader.GetInt32(2),
                await reader.IsDBNullAsync(3, cancellationToken).ConfigureAwait(false) ? null : reader.GetString(3), await reader.IsDBNullAsync(4, cancellationToken).ConfigureAwait(false) ? null : reader.GetString(4)));
        }
        return reports;
    }

    public async Task<bool> ContainsAsync(Guid sourceMessageId, CancellationToken cancellationToken) {
        NpgsqlCommand command = dataSource.CreateCommand("select exists(select 1 from bugtriage_reports where source_message_id = @source)");
        await using System.Runtime.CompilerServices.ConfiguredAsyncDisposable commandScope = command.ConfigureAwait(false);
        command.Parameters.AddWithValue("source", sourceMessageId);
        return (bool)(await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false))!;
    }

    public async Task ImportAsync(ImportedReport report, DateTimeOffset expiresAtUtc, CancellationToken cancellationToken) {
        NpgsqlCommand command = dataSource.CreateCommand("""
            insert into bugtriage_reports (id, source_message_id, received_at_utc, subject, text_body, raw_mime, expires_at_utc, status)
            values (@id, @source, @received, @subject, @body, @mime, @expires, @status)
            on conflict (source_message_id) do nothing
            """);
        await using System.Runtime.CompilerServices.ConfiguredAsyncDisposable commandScope = command.ConfigureAwait(false);
        command.Parameters.AddWithValue("id", Guid.NewGuid());
        command.Parameters.AddWithValue("source", report.SourceMessageId);
        command.Parameters.AddWithValue("received", report.ReceivedAtUtc);
        command.Parameters.AddWithValue("subject", report.Subject);
        command.Parameters.AddWithValue("body", report.TextBody);
        command.Parameters.AddWithValue("mime", NpgsqlDbType.Bytea, (object?)report.RawMime ?? DBNull.Value);
        command.Parameters.AddWithValue("expires", expiresAtUtc);
        command.Parameters.AddWithValue("status", report.RawMime is null ? "content_unavailable" : "pending");
        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<ReportLease?> ClaimAsync(DateTimeOffset now, TimeSpan duration, int maxAttempts, CancellationToken cancellationToken) {
        // Lock before taking the claim snapshot: concurrent service instances must share the same capacity limit.
        NpgsqlConnection connection = await dataSource.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        await using System.Runtime.CompilerServices.ConfiguredAsyncDisposable connectionScope = connection.ConfigureAwait(false);
        NpgsqlTransaction transaction = await connection.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
        await using System.Runtime.CompilerServices.ConfiguredAsyncDisposable transactionScope = transaction.ConfigureAwait(false);
        {
            var gate = new NpgsqlCommand("select pg_advisory_xact_lock(724936129)", connection, transaction);
            await using System.Runtime.CompilerServices.ConfiguredAsyncDisposable gateScope = gate.ConfigureAwait(false);
            await gate.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }
        NpgsqlCommand command = new("""
            with exhausted as (
                update bugtriage_reports set status = 'failed', summary = 'Attempt limit reached.',
                    lease_token = null, lease_expires_at_utc = null
                where status = 'in_progress' and lease_expires_at_utc <= @now and attempt >= @max_attempts
                returning id
            ), candidate as (
                select id from bugtriage_reports
                where (status = 'pending' or (status = 'in_progress' and lease_expires_at_utc <= @now))
                    and expires_at_utc > @now and attempt < @max_attempts
                    and (select count(*) from bugtriage_reports
                        where status = 'in_progress' and lease_expires_at_utc > @now and expires_at_utc > @now) < @capacity
                order by received_at_utc, id for update skip locked limit 1
            )
            update bugtriage_reports r set status = 'in_progress', attempt = attempt + 1,
                lease_token = @token, lease_expires_at_utc = least(@expires, expires_at_utc)
            from candidate c where r.id = c.id
            returning r.id, source_message_id, subject, text_body, lease_token, lease_expires_at_utc, attempt, expires_at_utc
            """, connection, transaction);
        await using System.Runtime.CompilerServices.ConfiguredAsyncDisposable commandScope = command.ConfigureAwait(false);
        command.Parameters.AddWithValue("now", now);
        command.Parameters.AddWithValue("expires", now.Add(duration));
        command.Parameters.AddWithValue("token", Guid.NewGuid());
        command.Parameters.AddWithValue("max_attempts", maxAttempts);
        command.Parameters.AddWithValue("capacity", options?.Value.MaxConcurrentReports ?? 1);
        NpgsqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        await using System.Runtime.CompilerServices.ConfiguredAsyncDisposable readerScope = reader.ConfigureAwait(false);
        ReportLease? lease = await reader.ReadAsync(cancellationToken).ConfigureAwait(false)
            ? new ReportLease(reader.GetGuid(0), reader.GetGuid(1), reader.GetString(2), reader.GetString(3),
                reader.GetGuid(4), await reader.GetFieldValueAsync<DateTimeOffset>(5, cancellationToken).ConfigureAwait(false), reader.GetInt32(6),
                await reader.GetFieldValueAsync<DateTimeOffset>(7, cancellationToken).ConfigureAwait(false)) : null;
        await reader.CloseAsync().ConfigureAwait(false);
        await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
        return lease;
    }

    public async Task<bool> RenewAsync(Guid id, Guid token, DateTimeOffset now, TimeSpan duration, CancellationToken cancellationToken) {
        NpgsqlCommand command = dataSource.CreateCommand("""
            update bugtriage_reports set lease_expires_at_utc = least(@expires, expires_at_utc)
            where id = @id and lease_token = @token and status = 'in_progress'
                and lease_expires_at_utc > @now and expires_at_utc > @now
            """);
        await using System.Runtime.CompilerServices.ConfiguredAsyncDisposable commandScope = command.ConfigureAwait(false);
        AddLeaseParameters(command, id, token, now);
        command.Parameters.AddWithValue("expires", now.Add(duration));
        return await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false) == 1;
    }

    public async Task<bool> CompleteAsync(Guid id, Guid token, ReportCompletion completion, DateTimeOffset now, CancellationToken cancellationToken) {
        if (!completion.IsValid()) {
            throw new ArgumentException("Invalid report completion.", nameof(completion));
        }
        // Identical retries succeed; an old worker can never overwrite a newer lease or completion.
        NpgsqlCommand command = dataSource.CreateCommand("""
            update bugtriage_reports set status = @outcome, summary = @summary, merge_request_url = @url,
                completed_token = @token, lease_token = null, lease_expires_at_utc = null
            where id = @id and expires_at_utc > @now and (
                (status = 'in_progress' and lease_token = @token and lease_expires_at_utc > @now)
                or (completed_token = @token and status = @outcome and summary = @summary and merge_request_url is not distinct from @url))
            """);
        await using System.Runtime.CompilerServices.ConfiguredAsyncDisposable commandScope = command.ConfigureAwait(false);
        AddLeaseParameters(command, id, token, now);
        command.Parameters.AddWithValue("outcome", completion.Outcome);
        command.Parameters.AddWithValue("summary", completion.Summary);
        command.Parameters.AddWithValue("url", NpgsqlDbType.Text, (object?)completion.MergeRequestUrl ?? DBNull.Value);
        return await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false) == 1;
    }

    public async Task<byte[]?> GetMimeAsync(Guid id, Guid token, DateTimeOffset now, CancellationToken cancellationToken) {
        NpgsqlCommand command = dataSource.CreateCommand("""
            select raw_mime from bugtriage_reports where id = @id and lease_token = @token
                and status = 'in_progress' and lease_expires_at_utc > @now and expires_at_utc > @now
            """);
        await using System.Runtime.CompilerServices.ConfiguredAsyncDisposable commandScope = command.ConfigureAwait(false);
        AddLeaseParameters(command, id, token, now);
        return await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false) as byte[];
    }

    public async Task PurgeAsync(DateTimeOffset now, CancellationToken cancellationToken) {
        NpgsqlCommand command = dataSource.CreateCommand("""
            update bugtriage_reports set subject = '', text_body = '', raw_mime = null, summary = null,
                merge_request_url = null, lease_token = null, lease_expires_at_utc = null, completed_token = null, status = 'expired'
            where expires_at_utc <= @now and status <> 'expired'
            """);
        await using System.Runtime.CompilerServices.ConfiguredAsyncDisposable commandScope = command.ConfigureAwait(false);
        command.Parameters.AddWithValue("now", now);
        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    private static void AddLeaseParameters(NpgsqlCommand command, Guid id, Guid token, DateTimeOffset now) {
        command.Parameters.AddWithValue("id", id);
        command.Parameters.AddWithValue("token", token);
        command.Parameters.AddWithValue("now", now);
    }
}
