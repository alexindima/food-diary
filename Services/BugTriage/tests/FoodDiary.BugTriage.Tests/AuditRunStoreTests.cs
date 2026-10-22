using FoodDiary.BugTriage.Application.AuditRuns;
using FoodDiary.BugTriage.Application.Reports;
using FoodDiary.BugTriage.Application.Reports.Identifiers;
using FoodDiary.BugTriage.Infrastructure.Options;
using FoodDiary.BugTriage.Infrastructure.Persistence;
using Microsoft.Extensions.Options;
using Npgsql;
using Testcontainers.PostgreSql;

namespace FoodDiary.BugTriage.Tests;

[Trait("Category", "Integration")]
[ExcludeFromCodeCoverage]
public sealed class AuditRunStoreTests : IAsyncLifetime {
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:17-alpine")
        .WithDatabase("fooddiary_bugtriage").Build();
    private NpgsqlDataSource _dataSource = null!;
    private NpgsqlAuditRunStore _store = null!;
    private static readonly DateTimeOffset Now = new(2026, 10, 12, 5, 0, 0, TimeSpan.Zero);
    private static readonly AuditWindow Window = new(Now.AddDays(-1), Now);

    public async Task InitializeAsync() {
        await _postgres.StartAsync().ConfigureAwait(false);
        _dataSource = NpgsqlDataSource.Create(_postgres.GetConnectionString());
        await BugTriageSchema.InitializeAsync(_dataSource, CancellationToken.None).ConfigureAwait(false);
        _store = new NpgsqlAuditRunStore(_dataSource, Options.Create(new BugTriageOptions()));
    }

    public async Task DisposeAsync() {
        await _dataSource.DisposeAsync().ConfigureAwait(false);
        await _postgres.DisposeAsync().ConfigureAwait(false);
    }

    [Fact]
    public async Task InvalidStart_CannotReplaceTheExistingRunningAuditOrItsInterval() {
        var original = (AuditRunId)Guid.NewGuid();
        Assert.True(await _store.StartAsync(original, Window, Now, CancellationToken.None));
        (AuditRunId Id, AuditWindow Window)[] invalid = [
            (new AuditRunId(Guid.Empty), Window),
            ((AuditRunId)Guid.NewGuid(), new AuditWindow(Now, Now)),
            ((AuditRunId)Guid.NewGuid(), new AuditWindow(Now, Now.AddMinutes(-1))),
            ((AuditRunId)Guid.NewGuid(), new AuditWindow(Now.AddDays(-7).AddTicks(-1), Now)),
            ((AuditRunId)Guid.NewGuid(), new AuditWindow(Now, Now.AddMinutes(5).AddTicks(1))),
            ((AuditRunId)Guid.NewGuid(), new AuditWindow(Now.AddDays(-90).AddTicks(-1), Now.AddDays(-89))),
        ];

        foreach ((AuditRunId id, AuditWindow window) in invalid) {
            ArgumentException error = await Assert.ThrowsAsync<ArgumentException>(() => _store.StartAsync(id, window, Now, CancellationToken.None));
            Assert.Equal("window", error.ParamName);
        }

        AuditRunSummary? latest = await _store.GetLatestAsync(Now, CancellationToken.None);
        Assert.NotNull(latest);
        Assert.Multiple(
            () => Assert.Equal(original.Value, latest.Id),
            () => Assert.Equal(Window.FromUtc, latest.FromUtc),
            () => Assert.Equal(Window.ToUtc, latest.ToUtc),
            () => Assert.Equal("running", latest.Status));
    }

    [Fact]
    public async Task ReorderedFindingRetry_IsIdenticalAndCannotDuplicateOrReplaceTheCompletedPublication() {
        var id = (AuditRunId)Guid.NewGuid();
        var first = new AuditFinding(Guid.Parse("10000000-0000-0000-0000-000000000001"), "First finding",
            ReportOutcome.NeedsInformation, "First evidence", MergeRequestUrl: null);
        var second = new AuditFinding(Guid.Parse("20000000-0000-0000-0000-000000000002"), "Second finding",
            ReportOutcome.DraftReady, "Second evidence", "https://github.com/example/repo/pull/2");
        var completion = new AuditCompletion("findings", "Two verified findings", [second, first]);
        Assert.True(await _store.StartAsync(id, Window, Now, CancellationToken.None));

        Assert.True(await _store.CompleteAsync(id, completion, Now.AddMinutes(1), CancellationToken.None));
        Assert.True(await _store.CompleteAsync(id, completion with { Findings = [first, second] }, Now.AddMinutes(2), CancellationToken.None));
        Assert.False(await _store.CompleteAsync(id, completion with { Summary = "Conflicting content" }, Now.AddMinutes(3), CancellationToken.None));

        BugReportJournalPage page = await Journal().GetPageAsync(Filter(), Now.AddMinutes(4), CancellationToken.None);
        Assert.Equal(2, page.TotalItems);
        Assert.Equal(new[] { first.Id, second.Id }, page.Items.Select(item => item.Id).Order());
        foreach (AuditFinding finding in completion.Findings) {
            BugReportJournalEntry entry = Assert.Single(page.Items, item => item.Id == finding.Id);
            Assert.Multiple(
                () => Assert.Equal(id.Value, entry.AuditRunId),
                () => Assert.Equal(finding.Title, entry.Subject),
                () => Assert.Equal(finding.Summary, entry.Summary),
                () => Assert.Equal(finding.MergeRequestUrl, entry.MergeRequestUrl));
        }
        Assert.NotNull(page.LatestAudit);
        Assert.Equal("Two verified findings", page.LatestAudit.Summary);
        Assert.Equal(Now.AddMinutes(1), page.LatestAudit.CompletedAtUtc);
    }

    [Fact]
    public async Task ConcurrentIdenticalPublication_CreatesOneFindingAndFencesConflictingRetries() {
        var id = (AuditRunId)Guid.NewGuid();
        bool[] starts = await Task.WhenAll(Enumerable.Range(0, 4).Select(_ => _store.StartAsync(id, Window, Now, CancellationToken.None)));
        Assert.All(starts, Assert.True);
        Assert.False(await _store.StartAsync(id, Window with { FromUtc = Now.AddHours(-2) }, Now, CancellationToken.None));
        var finding = new AuditFinding(Guid.NewGuid(), "Navigation failed", ReportOutcome.DraftReady, "Regression verified", "https://github.com/example/repo/pull/1");
        var completion = new AuditCompletion("findings", "One confirmed defect", [finding]);
        bool[] results = await Task.WhenAll(Enumerable.Range(0, 4).Select(_ => _store.CompleteAsync(id, completion, Now.AddMinutes(10), CancellationToken.None)));
        Assert.All(results, Assert.True);
        Assert.False(await _store.CompleteAsync(id, completion with { Summary = "Conflicting result" }, Now.AddMinutes(11), CancellationToken.None));
        BugReportJournalPage page = await Journal().GetPageAsync(Filter(), Now.AddMinutes(12), CancellationToken.None);
        Assert.Equal(1, page.TotalItems);
        BugReportJournalEntry entry = Assert.Single(page.Items);
        Assert.Equal(finding.Id, entry.Id);
        Assert.Null(entry.SourceMessageId);
        Assert.Equal("logs", entry.Source);
        Assert.Equal(id.Value, entry.AuditRunId);
        Assert.Equal(completion.Findings[0].MergeRequestUrl, entry.MergeRequestUrl);
        Assert.Equal(Now.AddMinutes(10), page.LatestAudit!.CompletedAtUtc);
    }

    [Fact]
    public async Task CleanAudit_DoesNotAddBugCards_AndIncompleteLatestPreservesLastSuccessfulTime() {
        var clean = (AuditRunId)Guid.NewGuid();
        await _store.StartAsync(clean, Window, Now, CancellationToken.None);
        await _store.CompleteAsync(clean, new AuditCompletion("clean", "No significant findings", []), Now.AddMinutes(5), CancellationToken.None);
        var partial = (AuditRunId)Guid.NewGuid();
        await _store.StartAsync(partial, new AuditWindow(Now, Now.AddDays(1)), Now.AddDays(1), CancellationToken.None);
        await _store.CompleteAsync(partial, new AuditCompletion("incomplete", "Loki unavailable", []), Now.AddDays(1).AddMinutes(3), CancellationToken.None);
        BugReportJournalPage page = await Journal().GetPageAsync(Filter() with { Search = "missing", Source = "email" }, Now.AddDays(1).AddMinutes(4), CancellationToken.None);
        Assert.Empty(page.Items);
        Assert.Equal("incomplete", page.LatestAudit!.Status);
        Assert.Equal(Now.AddMinutes(5), page.LatestAudit.LastSuccessfulAtUtc);
        Assert.False(page.LatestAudit.IsStale);
    }

    [Fact]
    public async Task CombinedJournal_AppliesSourceAndExclusivePeriodBeforePagination() {
        var mail = new NpgsqlBugReportStore(_dataSource);
        await mail.ImportAsync(new ImportedReport(SourceMessageId.New(), Now.AddMinutes(-1), "Email", "Body", [1]), Now.AddDays(1), CancellationToken.None);
        var id = (AuditRunId)Guid.NewGuid();
        await _store.StartAsync(id, new AuditWindow(Window.FromUtc.ToOffset(TimeSpan.FromHours(4)), Window.ToUtc.ToOffset(TimeSpan.FromHours(4))), Now, CancellationToken.None);
        await _store.CompleteAsync(id, new AuditCompletion("findings", "Needs review", [
            new AuditFinding(Guid.NewGuid(), "literal_%", ReportOutcome.NeedsInformation, "Sanitized evidence", MergeRequestUrl: null)]), Now.AddMinutes(2), CancellationToken.None);
        Assert.Equal(2, (await Journal().GetPageAsync(Filter(), Now.AddMinutes(3), CancellationToken.None)).TotalItems);
        BugReportJournalPage excluded = await Journal().GetPageAsync(Filter() with { FromUtc = Now.AddMinutes(-2), ToUtc = Now }, Now.AddMinutes(3), CancellationToken.None);
        Assert.Equal("email", Assert.Single(excluded.Items).Source);
        BugReportJournalPage logs = await Journal().GetPageAsync(Filter() with { FromUtc = Now, ToUtc = Now.AddMinutes(1), Source = "logs", Search = "_%" }, Now.AddMinutes(3), CancellationToken.None);
        BugReportJournalEntry entry = Assert.Single(logs.Items);
        Assert.Equal(Window.FromUtc, entry.AuditFromUtc);
        Assert.Equal(Window.ToUtc, entry.AuditToUtc);
    }

    [Fact]
    public async Task Expiry_RedactsBeforePurgeAndPreventsRevival_WhileKeepingReceipts() {
        var id = (AuditRunId)Guid.NewGuid();
        await _store.StartAsync(id, Window, Now, CancellationToken.None);
        var result = new AuditCompletion("findings", "Private summary", [
            new AuditFinding(Guid.NewGuid(), "Private title", ReportOutcome.DraftReady, "Private finding", "https://example.test/pull/1")]);
        await _store.CompleteAsync(id, result, Now.AddMinutes(1), CancellationToken.None);
        DateTimeOffset expiredAt = Now.AddDays(30);
        BugReportJournalPage page = await Journal().GetPageAsync(Filter(), expiredAt, CancellationToken.None);
        BugReportJournalEntry finding = Assert.Single(page.Items);
        Assert.True(finding.ContentExpired);
        Assert.Empty(finding.Subject);
        Assert.Null(finding.Summary);
        Assert.Null(finding.MergeRequestUrl);
        Assert.Null(page.LatestAudit!.Summary);
        Assert.Equal("expired", page.LatestAudit.Status);
        Assert.False(await _store.StartAsync(id, Window, expiredAt, CancellationToken.None));
        Assert.False(await _store.CompleteAsync(id, result, expiredAt, CancellationToken.None));
        await _store.PurgeAsync(expiredAt, CancellationToken.None);
        await using NpgsqlCommand command = _dataSource.CreateCommand("select completion is null from bugtriage_audit_runs where id = @id");
        command.Parameters.AddWithValue("id", id.Value);
        Assert.Equal(true, await command.ExecuteScalarAsync());
        Assert.Single((await Journal().GetPageAsync(Filter(), expiredAt, CancellationToken.None)).Items);
    }

    [Fact]
    public async Task InterruptedOrOverdueRun_IsExplicitlyStale() {
        var id = (AuditRunId)Guid.NewGuid();
        await _store.StartAsync(id, Window, Now, CancellationToken.None);
        Assert.False((await _store.GetLatestAsync(Now.AddHours(2), CancellationToken.None))!.IsStale);
        Assert.True((await _store.GetLatestAsync(Now.AddHours(2).AddMilliseconds(1), CancellationToken.None))!.IsStale);
        await _store.CompleteAsync(id, new AuditCompletion("clean", "Full coverage", []), Now.AddMinutes(5), CancellationToken.None);
        Assert.False((await _store.GetLatestAsync(Now.AddHours(36), CancellationToken.None))!.IsStale);
        Assert.True((await _store.GetLatestAsync(Now.AddHours(36).AddMilliseconds(1), CancellationToken.None))!.IsStale);
    }

    [Fact]
    public async Task InvalidCompletionOrForeignFindingId_CannotLeavePartialWrites() {
        var first = (AuditRunId)Guid.NewGuid();
        var second = (AuditRunId)Guid.NewGuid();
        await _store.StartAsync(first, Window, Now, CancellationToken.None);
        await _store.StartAsync(second, Window, Now.AddMinutes(1), CancellationToken.None);
        var finding = new AuditFinding(Guid.NewGuid(), "Existing finding", ReportOutcome.NeedsInformation, "Evidence", MergeRequestUrl: null);
        var completion = new AuditCompletion("findings", "Review", [finding]);
        await _store.CompleteAsync(first, completion, Now.AddMinutes(2), CancellationToken.None);
        Assert.False(await _store.CompleteAsync(second, completion, Now.AddMinutes(3), CancellationToken.None));
        Assert.Equal("running", (await _store.GetLatestAsync(Now.AddMinutes(4), CancellationToken.None))!.Status);
        Assert.Equal(1, (await Journal().GetPageAsync(Filter(), Now.AddMinutes(4), CancellationToken.None)).TotalItems);
        await Assert.ThrowsAsync<ArgumentException>(() => _store.CompleteAsync(second,
            new AuditCompletion("clean", "False clean", [finding]), Now, CancellationToken.None));
    }

    private NpgsqlBugReportJournal Journal() => new(_dataSource, _store);
    private static BugReportJournalFilter Filter() => new(1, 25, FromUtc: null, ToUtc: null, Status: null, Search: null, Id: null);
}
