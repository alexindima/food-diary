using System.Net;
using System.Net.Http.Json;
using FoodDiary.BugTriage.Application.Abstractions;
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

public sealed class BugReportsHttpTests {
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

    private sealed class Factory : WebApplicationFactory<Program> {
        public const string Key = "test-only-bugtriage-key-with-32-characters";

        protected override void ConfigureWebHost(IWebHostBuilder builder) {
            builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(new Dictionary<string, string?>(StringComparer.Ordinal) {
                ["BugTriageHttp:ApiKey"] = Key,
                ["MailInboxClient:BaseUrl"] = "https://mail.example.invalid",
                ["MailInboxClient:MetadataApiKey"] = new string('m', 32),
                ["MailInboxClient:ContentApiKey"] = new string('c', 32),
            }));
            builder.ConfigureTestServices(services => {
                services.RemoveAll<IBugReportStore>();
                services.AddSingleton(Substitute.For<IBugReportStore>());
                ServiceDescriptor? worker = services.SingleOrDefault(d => d.ServiceType == typeof(IHostedService) && d.ImplementationType == typeof(BugMailImportWorker));
                if (worker is not null) {
                    services.Remove(worker);
                }
            });
        }
    }
}
