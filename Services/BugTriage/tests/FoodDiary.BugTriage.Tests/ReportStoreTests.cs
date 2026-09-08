using FoodDiary.BugTriage.Application.Reports;
using FoodDiary.BugTriage.Infrastructure.Persistence;
using Npgsql;
using Testcontainers.PostgreSql;

namespace FoodDiary.BugTriage.Tests;

[Collection("BugTriage initialization environment")]
public sealed class ReportStoreTests : IAsyncLifetime {
    [Fact]
    public async Task Journal_FiltersBeforePagingAndRedactsExpiredContentBeforePurge() {
        await _store.ImportAsync(new ImportedReport(Guid.NewGuid(), Now.AddMinutes(-2), "literal_%", "private", [1]), Now.AddDays(1), CancellationToken.None);
        ReportLease lease = Assert.IsType<ReportLease>(await _store.ClaimAsync(Now, TimeSpan.FromMinutes(5), 3, CancellationToken.None));
        await _store.CompleteAsync(lease.Id, lease.LeaseToken, new ReportCompletion(ReportOutcome.DraftReady, "private summary", "https://example.test/pull/1"), Now, CancellationToken.None);
        await _store.ImportAsync(new ImportedReport(Guid.NewGuid(), Now.AddMinutes(-1), "literal_%", "private", [1]), Now.AddDays(1), CancellationToken.None);
        await _store.ImportAsync(new ImportedReport(Guid.NewGuid(), Now, "other", "private", [1]), Now.AddDays(1), CancellationToken.None);
        var journal = new NpgsqlBugReportJournal(_dataSource);
        var filter = new BugReportJournalFilter(Page: 2, Limit: 1, FromUtc: Now.AddMinutes(-3), ToUtc: Now,
            Status: null, Search: "_%", Id: null);

        BugReportJournalPage page = await journal.GetPageAsync(filter, Now, CancellationToken.None);

        Assert.Equal(2, page.TotalItems);
        Assert.Equal(lease.Id, Assert.Single(page.Items).Id);
        BugReportJournalPage expired = await journal.GetPageAsync(filter with { Page = 1, Search = null, Id = lease.Id }, Now.AddDays(2), CancellationToken.None);
        BugReportJournalEntry entry = Assert.Single(expired.Items);
        Assert.True(entry.ContentExpired);
        Assert.Equal("expired", entry.Status);
        Assert.Empty(entry.Subject);
        Assert.Null(entry.Summary);
        Assert.Null(entry.MergeRequestUrl);
        BugReportJournalPage hiddenSearch = await journal.GetPageAsync(filter, Now.AddDays(2), CancellationToken.None);
        Assert.Equal(0, hiddenSearch.TotalItems);
    }

    [Fact]
    public async Task InitializeEntryPoint_CreatesSchemaInProcess() {
        await using (NpgsqlCommand drop = _dataSource.CreateCommand("drop table bugtriage_reports")) {
            await drop.ExecuteNonQueryAsync();
        }
        const string connectionKey = "ConnectionStrings__BugTriage";
        string? previous = Environment.GetEnvironmentVariable(connectionKey);
        try {
            Environment.SetEnvironmentVariable(connectionKey, _postgres.GetConnectionString());
            System.Reflection.MethodInfo entryPoint = typeof(Program).Assembly.EntryPoint!;
            await Task.Run(() => entryPoint.Invoke(null, [new[] { "--initialize" }]));
            Assert.Empty(await _store.GetRecentAsync(Now, CancellationToken.None));
        } finally {
            Environment.SetEnvironmentVariable(connectionKey, previous);
        }
    }

    [Fact]
    public async Task InitializeCommand_CreatesSchemaAndExitsWithoutStartingServer() {
        await using (NpgsqlCommand drop = _dataSource.CreateCommand("drop table bugtriage_reports")) {
            await drop.ExecuteNonQueryAsync();
        }
        var start = new System.Diagnostics.ProcessStartInfo("dotnet") {
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };
        start.ArgumentList.Add(typeof(Program).Assembly.Location);
        start.ArgumentList.Add("--initialize");
        start.Environment["ConnectionStrings__BugTriage"] = _postgres.GetConnectionString();
        using System.Diagnostics.Process process = System.Diagnostics.Process.Start(start)!;
        Task<string> output = process.StandardOutput.ReadToEndAsync();
        Task<string> errors = process.StandardError.ReadToEndAsync();
        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        try {
            await process.WaitForExitAsync(deadline.Token);
        } finally {
            if (!process.HasExited) { process.Kill(entireProcessTree: true); }
        }

        Assert.Equal(0, process.ExitCode);
        Assert.DoesNotContain("Now listening", await output, StringComparison.Ordinal);
        Assert.Empty(await errors);
        Assert.Empty(await _store.GetRecentAsync(Now, CancellationToken.None));
    }
    [Fact]
    public async Task RecentReports_ExcludeExpiredOrderNewestFirstAndMapCompletion() {
        Assert.Empty(await _store.GetRecentAsync(Now, CancellationToken.None));
        await _store.ImportAsync(new ImportedReport(Guid.NewGuid(), Now.AddMinutes(-2), "older", "body", [1]), Now.AddDays(1), CancellationToken.None);
        ReportLease lease = Assert.IsType<ReportLease>(await _store.ClaimAsync(Now, TimeSpan.FromMinutes(5), 3, CancellationToken.None));
        var completion = new ReportCompletion(ReportOutcome.DraftReady, "Verified regression", "https://github.com/example/repo/pull/2");
        Assert.True(await _store.CompleteAsync(lease.Id, lease.LeaseToken, completion, Now, CancellationToken.None));
        await _store.ImportAsync(new ImportedReport(Guid.NewGuid(), Now.AddMinutes(-1), "newer", "body", [1]), Now.AddDays(1), CancellationToken.None);
        await _store.ImportAsync(new ImportedReport(Guid.NewGuid(), Now, "expired", "body", [1]), Now, CancellationToken.None);

        IReadOnlyList<ReportSummary> reports = await _store.GetRecentAsync(Now, CancellationToken.None);

        Assert.Equal(2, reports.Count);
        Assert.Multiple(
            () => Assert.Equal("pending", reports[0].Status),
            () => Assert.Equal(0, reports[0].Attempt),
            () => Assert.Null(reports[0].Summary),
            () => Assert.Null(reports[0].MergeRequestUrl),
            () => Assert.Equal(new ReportSummary(lease.Id, ReportOutcome.DraftReady, 1, completion.Summary, completion.MergeRequestUrl), reports[1]));
        for (int index = 0; index < 51; index++) {
            await _store.ImportAsync(new ImportedReport(Guid.NewGuid(), Now.AddSeconds(index), "new", "body", RawMime: null), Now.AddDays(1), CancellationToken.None);
        }
        Assert.Equal(50, (await _store.GetRecentAsync(Now, CancellationToken.None)).Count);
    }

    [Fact]
    public async Task CompleteAsync_RejectsInvalidOutcomeBeforeWriting() {
        await Assert.ThrowsAsync<ArgumentException>(() => _store.CompleteAsync(Guid.NewGuid(), Guid.NewGuid(),
            new ReportCompletion("invalid", "summary", MergeRequestUrl: null), Now, CancellationToken.None));
    }

    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:17-alpine")
        .WithDatabase("fooddiary_bugtriage").Build();
    private NpgsqlDataSource _dataSource = null!;
    private NpgsqlBugReportStore _store = null!;
    private static readonly DateTimeOffset Now = new(2026, 9, 7, 12, 0, 0, TimeSpan.Zero);

    public async Task InitializeAsync() {
        await _postgres.StartAsync().ConfigureAwait(false);
        _dataSource = NpgsqlDataSource.Create(_postgres.GetConnectionString());
        await BugTriageSchema.InitializeAsync(_dataSource, CancellationToken.None).ConfigureAwait(false);
        await BugTriageSchema.InitializeAsync(_dataSource, CancellationToken.None).ConfigureAwait(false);
        _store = new NpgsqlBugReportStore(_dataSource);
    }

    public async Task DisposeAsync() {
        await _dataSource.DisposeAsync().ConfigureAwait(false);
        await _postgres.DisposeAsync().ConfigureAwait(false);
    }

    [Fact]
    public async Task ConcurrentImportsAndClaims_ProduceOneLease_AndFenceStaleCompletion() {
        var report = new ImportedReport(Guid.NewGuid(), Now, "Broken button", "Steps", [1, 2, 3]);
        await Task.WhenAll(Enumerable.Range(0, 6).Select(_ => _store.ImportAsync(report, Now.AddDays(1), CancellationToken.None)));
        ReportLease?[] claims = await Task.WhenAll(Enumerable.Range(0, 6).Select(_ =>
            _store.ClaimAsync(Now, TimeSpan.FromMinutes(5), 3, CancellationToken.None)));
        ReportLease first = Assert.Single(claims.OfType<ReportLease>());
        ReportLease? second = await _store.ClaimAsync(Now.AddMinutes(6), TimeSpan.FromMinutes(5), 3, CancellationToken.None);
        Assert.NotNull(second);
        var completion = new ReportCompletion(ReportOutcome.DraftReady, "Regression tested", "https://github.com/example/repo/pull/1");
        Assert.False(await _store.CompleteAsync(first.Id, first.LeaseToken, completion, Now.AddMinutes(6), CancellationToken.None));
        Assert.False(await _store.RenewAsync(first.Id, first.LeaseToken, Now.AddMinutes(6), TimeSpan.FromMinutes(5), CancellationToken.None));
        Assert.True(await _store.CompleteAsync(second.Id, second.LeaseToken, completion, Now.AddMinutes(6), CancellationToken.None));
        Assert.True(await _store.CompleteAsync(second.Id, second.LeaseToken, completion, Now.AddMinutes(7), CancellationToken.None));
        Assert.False(await _store.CompleteAsync(second.Id, second.LeaseToken, completion with { Summary = "Changed" }, Now.AddMinutes(7), CancellationToken.None));
        Assert.Null(await _store.ClaimAsync(Now.AddHours(1), TimeSpan.FromMinutes(5), 3, CancellationToken.None));
    }

    [Fact]
    public async Task RetentionPurgesContentButKeepsReceipt_AndCannotReviveLease() {
        var report = new ImportedReport(Guid.NewGuid(), Now, "Private", "Private body", [5, 6, 7]);
        await _store.ImportAsync(report, Now.AddMinutes(3), CancellationToken.None);
        ReportLease? lease = await _store.ClaimAsync(Now, TimeSpan.FromMinutes(30), 3, CancellationToken.None);
        Assert.NotNull(lease);
        Assert.Equal(Now.AddMinutes(3), lease.LeaseExpiresAtUtc);
        Assert.Equal(report.RawMime, await _store.GetMimeAsync(lease.Id, lease.LeaseToken, Now, CancellationToken.None));
        Assert.Null(await _store.GetMimeAsync(lease.Id, Guid.NewGuid(), Now, CancellationToken.None));
        await _store.PurgeAsync(Now.AddMinutes(4), CancellationToken.None);
        Assert.True(await _store.ContainsAsync(report.SourceMessageId, CancellationToken.None));
        await _store.ImportAsync(report, Now.AddDays(1), CancellationToken.None);
        Assert.Null(await _store.ClaimAsync(Now.AddMinutes(4), TimeSpan.FromMinutes(30), 3, CancellationToken.None));
        Assert.Null(await _store.GetMimeAsync(lease.Id, lease.LeaseToken, Now.AddMinutes(4), CancellationToken.None));
        await using NpgsqlCommand command = _dataSource.CreateCommand("select subject, text_body, raw_mime, summary from bugtriage_reports");
        await using NpgsqlDataReader reader = await command.ExecuteReaderAsync();
        Assert.True(await reader.ReadAsync());
        Assert.Equal(string.Empty, reader.GetString(0));
        Assert.Equal(string.Empty, reader.GetString(1));
        Assert.True(await reader.IsDBNullAsync(2));
        Assert.True(await reader.IsDBNullAsync(3));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    public async Task ConcurrentClaimsAcrossDifferentReports_RespectCapacity_AndCompletionReleasesSlot(int capacity) {
        _store = new NpgsqlBugReportStore(_dataSource, Microsoft.Extensions.Options.Options.Create(
            new FoodDiary.BugTriage.Infrastructure.Options.BugTriageOptions { MaxConcurrentReports = capacity }));
        for (int index = 0; index < 6; index++) {
            await _store.ImportAsync(new ImportedReport(Guid.NewGuid(), Now.AddSeconds(index), "Bug", "Steps", [1]),
                Now.AddDays(1), CancellationToken.None);
        }
        ReportLease?[] claims = await Task.WhenAll(Enumerable.Range(0, 6).Select(_ =>
            _store.ClaimAsync(Now, TimeSpan.FromMinutes(5), 3, CancellationToken.None)));
        ReportLease[] active = [.. claims.OfType<ReportLease>()];
        Assert.Equal(capacity, active.Length);
        ReportLease first = active[0];
        Assert.True(await _store.CompleteAsync(first.Id, first.LeaseToken,
            new ReportCompletion(ReportOutcome.NotConfirmed, "Checked", MergeRequestUrl: null), Now, CancellationToken.None));
        ReportLease? next = await _store.ClaimAsync(Now, TimeSpan.FromMinutes(5), 3, CancellationToken.None);
        Assert.NotNull(next);
        Assert.NotEqual(first.Id, next.Id);
    }

    [Fact]
    public async Task ExpiredAttemptsStopAtConfiguredLimit() {
        await _store.ImportAsync(new ImportedReport(Guid.NewGuid(), Now, "Bug", "Steps", [1]), Now.AddDays(1), CancellationToken.None);
        Assert.NotNull(await _store.ClaimAsync(Now, TimeSpan.FromMinutes(1), 1, CancellationToken.None));
        Assert.Null(await _store.ClaimAsync(Now.AddMinutes(2), TimeSpan.FromMinutes(1), 1, CancellationToken.None));
        await using NpgsqlCommand command = _dataSource.CreateCommand("select status from bugtriage_reports");
        Assert.Equal("failed", await command.ExecuteScalarAsync());
    }
}
