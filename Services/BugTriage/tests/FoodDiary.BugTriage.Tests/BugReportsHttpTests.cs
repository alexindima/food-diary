using System.Net;
using System.Net.Http.Json;
using FoodDiary.BugTriage.Application.Abstractions;
using FoodDiary.BugTriage.Application.Reports;
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
    public async Task LeaseEndpoints_ReturnContentRenewCompleteAndProtectFailures() {
        await using var factory = new Factory();
        var id = Guid.NewGuid();
        var token = Guid.NewGuid();
        factory.Store.GetMimeAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult<byte[]?>(result: null));
        factory.Store.RenewAsync(id, token, Arg.Any<DateTimeOffset>(), Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>()).Returns(returnThis: true);
        factory.Store.CompleteAsync(id, token, Arg.Any<ReportCompletion>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>()).Returns(returnThis: true);
        factory.Store.GetMimeAsync(id, token, Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>()).Returns([0, 255, 13, 10]);
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

    [ExcludeFromCodeCoverage]
    private sealed class Factory : WebApplicationFactory<Program> {
        public const string Key = "test-only-bugtriage-key-with-32-characters";
        public IBugReportStore Store { get; } = Substitute.For<IBugReportStore>();

        protected override void ConfigureWebHost(IWebHostBuilder builder) {
            builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(new Dictionary<string, string?>(StringComparer.Ordinal) {
                ["BugTriageHttp:ApiKey"] = Key,
                ["MailInboxClient:BaseUrl"] = "https://mail.example.invalid",
                ["MailInboxClient:MetadataApiKey"] = new string('m', 32),
                ["MailInboxClient:ContentApiKey"] = new string('c', 32),
            }));
            builder.ConfigureTestServices(services => {
                services.RemoveAll<IBugReportStore>();
                services.AddSingleton(Store);
                ServiceDescriptor? worker = services.SingleOrDefault(d => d.ServiceType == typeof(IHostedService) && d.ImplementationType == typeof(BugMailImportWorker));
                if (worker is not null) {
                    services.Remove(worker);
                }
            });
        }
    }
}
