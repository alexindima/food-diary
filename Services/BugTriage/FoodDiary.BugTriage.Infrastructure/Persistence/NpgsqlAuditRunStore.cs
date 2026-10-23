using System.Runtime.CompilerServices;
using System.Text.Json;
using FoodDiary.BugTriage.Application.Abstractions;
using FoodDiary.BugTriage.Application.AuditRuns;
using FoodDiary.BugTriage.Infrastructure.Options;
using Microsoft.Extensions.Options;
using Npgsql;
using NpgsqlTypes;

namespace FoodDiary.BugTriage.Infrastructure.Persistence;

public sealed class NpgsqlAuditRunStore(NpgsqlDataSource dataSource, IOptions<BugTriageOptions> options) : IAuditRunStore {
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<bool> StartAsync(AuditRunId id, AuditWindow window, DateTimeOffset now, CancellationToken cancellationToken) {
        if (id.Value == Guid.Empty || !window.IsValid(now)) {
            throw new ArgumentException("Invalid audit start.", nameof(window));
        }
        NpgsqlCommand command = dataSource.CreateCommand("""
            insert into bugtriage_audit_runs (id, from_utc, to_utc, started_at_utc, expires_at_utc)
            values (@id, @from, @to, @now, @expires)
            on conflict (id) do update set id = excluded.id
                where bugtriage_audit_runs.from_utc = excluded.from_utc and bugtriage_audit_runs.to_utc = excluded.to_utc
                    and bugtriage_audit_runs.expires_at_utc > @now
            """);
        await using ConfiguredAsyncDisposable commandScope = command.ConfigureAwait(false);
        command.Parameters.AddWithValue("id", id.Value);
        command.Parameters.AddWithValue("from", window.FromUtc.ToUniversalTime());
        command.Parameters.AddWithValue("to", window.ToUtc.ToUniversalTime());
        command.Parameters.AddWithValue("now", now);
        command.Parameters.AddWithValue("expires", now.Add(options.Value.ContentRetention));
        return await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false) == 1;
    }

    public async Task<bool> CompleteAsync(AuditRunId id, AuditCompletion completion, DateTimeOffset now, CancellationToken cancellationToken) {
        if (id.Value == Guid.Empty || !completion.IsValid()) {
            throw new ArgumentException("Invalid audit completion.", nameof(completion));
        }
        string json = JsonSerializer.Serialize(completion with { Findings = completion.Findings.OrderBy(finding => finding.Id).ToArray() }, JsonOptions);
        NpgsqlConnection connection = await dataSource.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        await using ConfiguredAsyncDisposable connectionScope = connection.ConfigureAwait(false);
        NpgsqlTransaction transaction = await connection.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
        await using ConfiguredAsyncDisposable transactionScope = transaction.ConfigureAwait(false);
        bool running;
        bool identical;
        {
            var state = new NpgsqlCommand("""
                select status = 'running', completion = @completion
                from bugtriage_audit_runs where id = @id and expires_at_utc > @now for update
                """, connection, transaction);
            await using ConfiguredAsyncDisposable stateScope = state.ConfigureAwait(false);
            state.Parameters.AddWithValue("id", id.Value);
            state.Parameters.AddWithValue("now", now);
            state.Parameters.AddWithValue("completion", NpgsqlDbType.Jsonb, json);
            NpgsqlDataReader reader = await state.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
            await using ConfiguredAsyncDisposable readerScope = reader.ConfigureAwait(false);
            if (!await reader.ReadAsync(cancellationToken).ConfigureAwait(false)) {
                return false;
            }
            running = reader.GetBoolean(0);
            identical = !await reader.IsDBNullAsync(1, cancellationToken).ConfigureAwait(false) && reader.GetBoolean(1);
        }
        if (!running) {
            return identical;
        }
        foreach (AuditFinding finding in completion.Findings) {
            var insert = new NpgsqlCommand("""
                insert into bugtriage_audit_findings (id, audit_run_id, title, status, summary, merge_request_url)
                values (@id, @run, @title, @status, @summary, @url)
                """, connection, transaction);
            await using ConfiguredAsyncDisposable insertScope = insert.ConfigureAwait(false);
            insert.Parameters.AddWithValue("id", finding.Id);
            insert.Parameters.AddWithValue("run", id.Value);
            insert.Parameters.AddWithValue("title", finding.Title);
            insert.Parameters.AddWithValue("status", finding.Outcome);
            insert.Parameters.AddWithValue("summary", finding.Summary);
            insert.Parameters.AddWithValue("url", NpgsqlDbType.Text, (object?)finding.MergeRequestUrl ?? DBNull.Value);
            try {
                await insert.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
            } catch (PostgresException exception) when (exception.SqlState is PostgresErrorCodes.UniqueViolation) {
                return false;
            }
        }
        var update = new NpgsqlCommand("""
            update bugtriage_audit_runs set status = @status, summary = @summary, completed_at_utc = @now,
                finding_count = @count, coverage_complete = @covered, completion = @completion where id = @id
            """, connection, transaction);
        await using ConfiguredAsyncDisposable updateScope = update.ConfigureAwait(false);
        update.Parameters.AddWithValue("id", id.Value);
        update.Parameters.AddWithValue("status", completion.Outcome);
        update.Parameters.AddWithValue("summary", completion.Summary);
        update.Parameters.AddWithValue("now", now);
        update.Parameters.AddWithValue("count", completion.Findings.Count);
        update.Parameters.AddWithValue("covered", completion.Outcome is "clean" or "findings");
        update.Parameters.AddWithValue("completion", NpgsqlDbType.Jsonb, json);
        await update.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
        return true;
    }

    public async Task<AuditRunSummary?> GetLatestAsync(DateTimeOffset now, CancellationToken cancellationToken) {
        NpgsqlCommand command = dataSource.CreateCommand("""
            select id, from_utc, to_utc, started_at_utc, completed_at_utc,
                case when expires_at_utc <= @now then 'expired' else status end,
                case when expires_at_utc > @now then summary else null end, finding_count, expires_at_utc <= @now,
                to_utc < @stale or (status = 'running' and started_at_utc < @interrupted),
                (select max(completed_at_utc) from bugtriage_audit_runs where coverage_complete)
            from bugtriage_audit_runs order by started_at_utc desc, id desc limit 1
            """);
        await using ConfiguredAsyncDisposable commandScope = command.ConfigureAwait(false);
        command.Parameters.AddWithValue("now", now);
        command.Parameters.AddWithValue("stale", now.AddHours(-36));
        command.Parameters.AddWithValue("interrupted", now.AddHours(-2));
        NpgsqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        await using ConfiguredAsyncDisposable readerScope = reader.ConfigureAwait(false);
        if (!await reader.ReadAsync(cancellationToken).ConfigureAwait(false)) {
            return null;
        }
        return new AuditRunSummary(reader.GetGuid(0),
            await reader.GetFieldValueAsync<DateTimeOffset>(1, cancellationToken).ConfigureAwait(false),
            await reader.GetFieldValueAsync<DateTimeOffset>(2, cancellationToken).ConfigureAwait(false),
            await reader.GetFieldValueAsync<DateTimeOffset>(3, cancellationToken).ConfigureAwait(false),
            await ReadNullableInstantAsync(reader, 4, cancellationToken).ConfigureAwait(false),
            reader.GetString(5), await reader.IsDBNullAsync(6, cancellationToken).ConfigureAwait(false) ? null : reader.GetString(6),
            reader.GetInt32(7), reader.GetBoolean(8), reader.GetBoolean(9),
            await ReadNullableInstantAsync(reader, 10, cancellationToken).ConfigureAwait(false));
    }

    private static async Task<DateTimeOffset?> ReadNullableInstantAsync(NpgsqlDataReader reader, int column, CancellationToken cancellationToken) =>
        await reader.IsDBNullAsync(column, cancellationToken).ConfigureAwait(false) ? null :
            await reader.GetFieldValueAsync<DateTimeOffset>(column, cancellationToken).ConfigureAwait(false);

    public async Task PurgeAsync(DateTimeOffset now, CancellationToken cancellationToken) {
        NpgsqlCommand command = dataSource.CreateCommand("""
            begin;
            update bugtriage_audit_findings set title = '', summary = null, merge_request_url = null, status = 'expired'
                where status <> 'expired' and audit_run_id in (select id from bugtriage_audit_runs where expires_at_utc <= @now);
            update bugtriage_audit_runs set summary = null, completion = null, status = 'expired'
                where expires_at_utc <= @now and status <> 'expired';
            commit;
            """);
        await using ConfiguredAsyncDisposable commandScope = command.ConfigureAwait(false);
        command.Parameters.AddWithValue("now", now);
        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }
}
