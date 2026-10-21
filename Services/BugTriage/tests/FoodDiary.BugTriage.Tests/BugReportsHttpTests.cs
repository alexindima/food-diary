using FoodDiary.BugTriage.Application.Reports.Identifiers;
using FoodDiary.BugTriage.Application.AuditRuns;
using System.Net;
using System.Net.Http.Json;
using FoodDiary.BugTriage.Application.Abstractions;
using FoodDiary.BugTriage.Application.Reports;
using FoodDiary.BugTriage.Presentation.Features.Reports;
using FoodDiary.BugTriage.Infrastructure.Workers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using NSubstitute;

namespace FoodDiary.BugTriage.Tests;

[ExcludeFromCodeCoverage]
public sealed class BugReportsHttpTests {
    [Fact]
    public async Task Claim_ReturnsTheLeasedReportIdentityContentAndFencingMetadataWithoutCaching() {
        await using var factory = new Factory();
        var now = new DateTimeOffset(2026, 10, 21, 12, 0, 0, TimeSpan.Zero);
        var lease = new ReportLease(new BugReportId(Guid.NewGuid()), new SourceMessageId(Guid.NewGuid()),
            "Fixture subject", "Fixture report text", new LeaseToken(Guid.NewGuid()), now.AddMinutes(5), 3, now.AddDays(1));
        factory.Store.ClaimAsync(Arg.Any<DateTimeOffset>(), Arg.Any<TimeSpan>(), Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(lease);
        using HttpClient client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-BugTriage-Key", Factory.Key);

        using HttpResponseMessage response = await client.PostAsync("/api/bug-reports/claim", content: null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(response.Headers.CacheControl?.NoStore);
        ReportLeaseHttpResponse? body = await response.Content.ReadFromJsonAsync<ReportLeaseHttpResponse>();
        Assert.NotNull(body);
        Assert.Multiple(
            () => Assert.Equal(lease.Id.Value, body.Id),
            () => Assert.Equal(lease.SourceMessageId.Value, body.SourceMessageId),
            () => Assert.Equal("Fixture subject", body.Subject),
            () => Assert.Equal("Fixture report text", body.TextBody),
            () => Assert.Equal(lease.LeaseToken.Value, body.LeaseToken),
            () => Assert.Equal(now.AddMinutes(5), body.LeaseExpiresAtUtc),
            () => Assert.Equal(3, body.Attempt),
            () => Assert.Equal(now.AddDays(1), body.ContentExpiresAtUtc));
        await factory.Store.Received(1).ClaimAsync(Arg.Any<DateTimeOffset>(), Arg.Any<TimeSpan>(), Arg.Any<int>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Journal_UsesSeparateReadCredentialAndRejectsWorkerOperations() {
        await using var factory = new Factory();
        factory.Journal.GetPageAsync(Arg.Any<BugReportJournalFilter>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(new BugReportJournalPage([], 0));
        using HttpClient client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-BugTriage-Read-Key", Factory.ReadKey);
        using HttpResponseMessage journal = await client.GetAsync("/api/report-journal?page=2&limit=10&status=not_confirmed");
        Assert.Equal(HttpStatusCode.OK, journal.StatusCode);
        Assert.True(journal.Headers.CacheControl?.NoStore);
        await factory.Journal.Received(1).GetPageAsync(Arg.Is<BugReportJournalFilter>(filter =>
            filter.Page == 2 && filter.Limit == 10 && filter.Status == "not_confirmed"), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>());

        client.DefaultRequestHeaders.Add("X-BugTriage-Key", Factory.ReadKey);
        using HttpResponseMessage claim = await client.PostAsync("/api/bug-reports/claim", content: null);
        Assert.Equal(HttpStatusCode.Unauthorized, claim.StatusCode);
        client.DefaultRequestHeaders.Remove("X-BugTriage-Read-Key");
        client.DefaultRequestHeaders.Add("X-BugTriage-Read-Key", Factory.Key);
        using HttpResponseMessage wrongKey = await client.GetAsync("/api/report-journal");
        Assert.Equal(HttpStatusCode.Unauthorized, wrongKey.StatusCode);
    }

    [Fact]
    public async Task ClientCancellation_AbortsPendingRequestWithoutServiceErrorResponse() {
        await using var factory = new Factory();
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var canceled = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        factory.Store.GetRecentAsync(Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>()).Returns<Task<IReadOnlyList<ReportSummary>>>(async call => {
            entered.TrySetResult();
            try {
                await Task.Delay(Timeout.InfiniteTimeSpan, call.Arg<CancellationToken>()).ConfigureAwait(false);
            } finally {
                canceled.TrySetResult();
            }
            return Array.Empty<ReportSummary>();
        });
        using HttpClient client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-BugTriage-Key", Factory.Key);
        using var cancellation = new CancellationTokenSource();
        Task<HttpResponseMessage> request = client.GetAsync("/api/bug-reports", cancellation.Token);
        await entered.Task.WaitAsync(TimeSpan.FromSeconds(10));

        await cancellation.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => request);
        await canceled.Task.WaitAsync(TimeSpan.FromSeconds(10));
    }
    [Fact]
    public async Task RecentReports_ReturnsStoreResultsWithoutCaching() {
        await using var factory = new Factory();
        factory.Store.GetRecentAsync(Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>()).Returns([]);
        using HttpClient client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-BugTriage-Key", Factory.Key);

        using HttpResponseMessage response = await client.GetAsync("/api/bug-reports");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("[]", await response.Content.ReadAsStringAsync());
        Assert.True(response.Headers.CacheControl?.NoStore);
        await factory.Store.Received(1).GetRecentAsync(Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>());
    }
    [Fact]
    public async Task LeaseEndpoints_ReturnContentRenewCompleteAndProtectFailures() {
        await using var factory = new Factory();
        var id = Guid.NewGuid();
        var token = Guid.NewGuid();
        factory.Store.GetMimeAsync(Arg.Any<BugReportId>(), Arg.Any<LeaseToken>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult<byte[]?>(result: null));
        factory.Store.RenewAsync((BugReportId)id, (LeaseToken)token, Arg.Any<DateTimeOffset>(), Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>()).Returns(returnThis: true);
        factory.Store.CompleteAsync((BugReportId)id, (LeaseToken)token, Arg.Any<ReportCompletion>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>()).Returns(returnThis: true);
        factory.Store.GetMimeAsync((BugReportId)id, (LeaseToken)token, Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>()).Returns([0, 255, 13, 10]);
        using HttpClient client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-BugTriage-Key", Factory.Key);
        client.DefaultRequestHeaders.Add("X-BugTriage-Lease", token.ToString());

        using HttpResponseMessage renewed = await client.PostAsJsonAsync($"/api/bug-reports/{id}/renew", new { leaseToken = token });
        using HttpResponseMessage stale = await client.PostAsJsonAsync($"/api/bug-reports/{id}/renew", new { leaseToken = Guid.NewGuid() });
        using HttpResponseMessage completed = await client.PostAsJsonAsync($"/api/bug-reports/{id}/complete",
            new { leaseToken = token, outcome = "not_confirmed", summary = "No reproducible defect", mergeRequestUrl = (string?)null });
        using HttpResponseMessage mime = await client.GetAsync($"/api/bug-reports/{id}/mime");
        using HttpResponseMessage missing = await client.GetAsync($"/api/bug-reports/{Guid.NewGuid()}/mime");

        Assert.Multiple(
            () => Assert.Equal(HttpStatusCode.NoContent, renewed.StatusCode),
            () => Assert.Equal(HttpStatusCode.Conflict, stale.StatusCode),
            () => Assert.Equal(HttpStatusCode.NoContent, completed.StatusCode),
            () => Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode),
            () => Assert.Equal("application/octet-stream", mime.Content.Headers.ContentType?.MediaType));
        Assert.Equal(new byte[] { 0, 255, 13, 10 }, await mime.Content.ReadAsByteArrayAsync());

        factory.Store.ClaimAsync(Arg.Any<DateTimeOffset>(), Arg.Any<TimeSpan>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<ReportLease?>(new InvalidOperationException("private@example.test password=secret")));
        using HttpResponseMessage failed = await client.PostAsync("/api/bug-reports/claim", content: null);
        Assert.Equal(HttpStatusCode.ServiceUnavailable, failed.StatusCode);
        Assert.Equal("{\"error\":\"Service temporarily unavailable.\"}", await failed.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Completion_ValidatesOptionalDisplayMetadataAndPreservesDetailedEvidence() {
        await using var factory = new Factory();
        var id = Guid.NewGuid();
        var token = Guid.NewGuid();
        factory.Store.CompleteAsync((BugReportId)id, (LeaseToken)token, Arg.Any<ReportCompletion>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>()).Returns(returnThis: true);
        using HttpClient client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-BugTriage-Key", Factory.Key);

        using HttpResponseMessage completed = await client.PostAsJsonAsync($"/api/bug-reports/{id}/complete",
            new { leaseToken = token, outcome = "not_confirmed", summary = "Full reproduction evidence", title = " Broken button ", shortSummary = " Cannot reproduce yet " });
        Assert.Equal(HttpStatusCode.NoContent, completed.StatusCode);
        await factory.Store.Received(1).CompleteAsync((BugReportId)id, (LeaseToken)token,
            Arg.Is<ReportCompletion>(completion => completion.Title == "Broken button" && completion.ShortSummary == "Cannot reproduce yet" && completion.Summary == "Full reproduction evidence"),
            Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>());

        foreach ((string title, string shortSummary) in new[] { (" ", "Valid"), (new string('t', 241), "Valid"), ("Valid", " "), ("Valid", new string('s', 601)) }) {
            using HttpResponseMessage invalid = await client.PostAsJsonAsync($"/api/bug-reports/{id}/complete",
                new { leaseToken = token, outcome = "not_confirmed", summary = "Full evidence", title, shortSummary });
            Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);
        }
        await factory.Store.Received(1).CompleteAsync(Arg.Any<BugReportId>(), Arg.Any<LeaseToken>(), Arg.Any<ReportCompletion>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task EndpointsEnforceAuthorizationValidationAndLeaseConflicts() {
        await using var factory = new Factory();
        using HttpClient client = factory.CreateClient();
        using HttpResponseMessage unauthorized = await client.PostAsync("/api/bug-reports/claim", content: null);
        Assert.Equal(HttpStatusCode.Unauthorized, unauthorized.StatusCode);
        client.DefaultRequestHeaders.Add("X-BugTriage-Key", Factory.Key);
        using HttpResponseMessage empty = await client.PostAsync("/api/bug-reports/claim", content: null);
        Assert.Equal(HttpStatusCode.NoContent, empty.StatusCode);
        var id = Guid.NewGuid();
        using HttpResponseMessage invalid = await client.PostAsJsonAsync($"/api/bug-reports/{id}/complete",
            new { leaseToken = Guid.NewGuid(), outcome = "draft_ready", summary = "Fix", mergeRequestUrl = (string?)null });
        Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);
        using HttpResponseMessage conflict = await client.PostAsJsonAsync($"/api/bug-reports/{id}/complete",
            new { leaseToken = Guid.NewGuid(), outcome = "not_confirmed", summary = "Could not reproduce", mergeRequestUrl = (string?)null });
        Assert.Equal(HttpStatusCode.Conflict, conflict.StatusCode);
        Assert.True(conflict.Headers.CacheControl?.NoStore);
    }

    [Fact]
    public async Task AuditWrites_RequireWorkerCredentialAndValidateIntervalOutcomeAndConflicts() {
        await using var factory = new Factory();
        using HttpClient client = factory.CreateClient();
        var id = Guid.NewGuid();
        DateTimeOffset now = DateTimeOffset.UtcNow;
        var start = new { fromUtc = now.AddDays(-1), toUtc = now };
        using HttpResponseMessage unauthorized = await client.PutAsJsonAsync($"/api/audit-runs/{id}/start", start);
        Assert.Equal(HttpStatusCode.Unauthorized, unauthorized.StatusCode);
        client.DefaultRequestHeaders.Add("X-BugTriage-Key", Factory.ReadKey);
        using HttpResponseMessage readOnly = await client.PutAsJsonAsync($"/api/audit-runs/{id}/start", start);
        Assert.Equal(HttpStatusCode.Unauthorized, readOnly.StatusCode);
        client.DefaultRequestHeaders.Remove("X-BugTriage-Key");
        client.DefaultRequestHeaders.Add("X-BugTriage-Key", Factory.Key);
        factory.AuditRuns.StartAsync((AuditRunId)id, Arg.Any<AuditWindow>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>()).Returns(returnThis: true);
        factory.AuditRuns.CompleteAsync((AuditRunId)id, Arg.Any<AuditCompletion>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>()).Returns(returnThis: true);
        using HttpResponseMessage started = await client.PutAsJsonAsync($"/api/audit-runs/{id}/start", start);
        Assert.Equal(HttpStatusCode.NoContent, started.StatusCode);
        Assert.True(started.Headers.CacheControl?.NoStore);
        using HttpResponseMessage invalidWindow = await client.PutAsJsonAsync($"/api/audit-runs/{id}/start", new { fromUtc = now, toUtc = now });
        Assert.Equal(HttpStatusCode.BadRequest, invalidWindow.StatusCode);
        using HttpResponseMessage completed = await client.PutAsJsonAsync($"/api/audit-runs/{id}/complete", new { outcome = "clean", summary = "No findings", findings = Array.Empty<object>() });
        Assert.Equal(HttpStatusCode.NoContent, completed.StatusCode);
        using HttpResponseMessage invalid = await client.PutAsJsonAsync($"/api/audit-runs/{id}/complete", new { outcome = "findings", summary = "Missing findings", findings = Array.Empty<object>() });
        Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);
        using HttpResponseMessage missing = await client.PutAsJsonAsync($"/api/audit-runs/{Guid.NewGuid()}/complete", new { outcome = "clean", summary = "No findings", findings = Array.Empty<object>() });
        Assert.Equal(HttpStatusCode.Conflict, missing.StatusCode);
    }

    [Fact]
    public async Task Journal_ReadsAuditMetadataAndSourceFilterWithoutWorkerCredentials() {
        await using var factory = new Factory();
        DateTimeOffset now = DateTimeOffset.UtcNow;
        var latest = new AuditRunSummary(Guid.NewGuid(), now.AddDays(-1), now, now, now, "incomplete", "Source unavailable", 0, ContentExpired: false, IsStale: false, LastSuccessfulAtUtc: null);
        factory.Journal.GetPageAsync(Arg.Any<BugReportJournalFilter>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(new BugReportJournalPage([], 0) { LatestAudit = latest });
        using HttpClient client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-BugTriage-Read-Key", Factory.ReadKey);
        BugReportJournalPage? page = await client.GetFromJsonAsync<BugReportJournalPage>("/api/report-journal/all?source=logs");
        Assert.Equal(latest, page!.LatestAudit);
        await factory.Journal.Received(1).GetPageAsync(Arg.Is<BugReportJournalFilter>(filter => filter.Source == "logs"), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>());
        using HttpResponseMessage legacy = await client.GetAsync("/api/report-journal?source=logs");
        Assert.Equal(HttpStatusCode.OK, legacy.StatusCode);
        await factory.Journal.Received(1).GetPageAsync(Arg.Is<BugReportJournalFilter>(filter => filter.Source == "email"), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>());
        using HttpResponseMessage invalid = await client.GetAsync("/api/report-journal/all?source=other");
        Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);
    }

    [ExcludeFromCodeCoverage]
    private sealed class Factory : WebApplicationFactory<Program> {
        public const string Key = "test-only-bugtriage-key-with-32-characters";
        public const string ReadKey = "test-only-read-journal-key-with-32-characters";
        public IBugReportStore Store { get; } = Substitute.For<IBugReportStore>();
        public IBugReportJournal Journal { get; } = Substitute.For<IBugReportJournal>();
        public IAuditRunStore AuditRuns { get; } = Substitute.For<IAuditRunStore>();

        protected override void ConfigureWebHost(IWebHostBuilder builder) {
            builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(new Dictionary<string, string?>(StringComparer.Ordinal) {
                ["BugTriageHttp:ApiKey"] = Key,
                ["BugTriageHttp:ReadApiKey"] = ReadKey,
                ["MailInboxClient:BaseUrl"] = "https://mail.example.invalid",
                ["MailInboxClient:MetadataApiKey"] = new string('m', 32),
                ["MailInboxClient:ContentApiKey"] = new string('c', 32),
            }));
            builder.ConfigureTestServices(services => {
                services.RemoveAll<IBugReportStore>();
                services.AddSingleton(Store);
                services.RemoveAll<IBugReportJournal>();
                services.AddSingleton(Journal);
                services.RemoveAll<IAuditRunStore>();
                services.AddSingleton(AuditRuns);
                ServiceDescriptor[] workers = [.. services.Where(d => d.ServiceType == typeof(IHostedService) &&
                    (d.ImplementationType == typeof(BugMailImportWorker) || d.ImplementationType == typeof(AuditRetentionWorker)))];
                foreach (ServiceDescriptor worker in workers) {
                    services.Remove(worker);
                }
            });
        }
    }
}
