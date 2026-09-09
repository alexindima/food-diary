using System.Net;
using System.Net.Http.Json;
using FoodDiary.Application.Abstractions.Admin.Common;
using FoodDiary.Application.Abstractions.Admin.Models;
using FoodDiary.Infrastructure.Integrations;
using FoodDiary.Infrastructure.Integrations.BugTriage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace FoodDiary.Modules.Admin.Infrastructure.Tests.Integrations;

[ExcludeFromCodeCoverage]
public sealed class AdminBugReportReaderTests {
    [Theory]
    [InlineData("", "", false, true)]
    [InlineData("https://bugs.example.com", "", false, false)]
    [InlineData("https://bugs.example.com", "valid", false, true)]
    [InlineData("http://127.0.0.1:5099", "valid", true, true)]
    [InlineData("http://127.0.0.1:5099", "valid", false, false)]
    [InlineData("http://bugs.example.com", "valid", true, false)]
    [InlineData("https://user@bugs.example.com", "valid", false, false)]
    [InlineData("https://bugs.example.com?q=1", "valid", false, false)]
    [InlineData("https://bugs.example.com/#fragment", "valid", false, false)]
    public void Options_RequireTrustedEndpointAndReadKey(string url, string key, bool loopback, bool valid) {
        Assert.Equal(valid, AdminBugTriageOptions.IsValid(new AdminBugTriageOptions { BaseUrl = url, ReadApiKey = key.Length == 0 ? key : new string('k', 32), AllowInsecureLoopback = loopback }));
    }

    [Fact]
    public async Task Unconfigured_ReturnsExplicitlyUnavailablePageWithoutHttp() {
        using var handler = new ResponseHandler(() => throw new InvalidOperationException("HTTP must not be called"));
        using var client = new HttpClient(handler);
        var reader = new AdminBugReportReader(client, Options.Create(new AdminBugTriageOptions()));
        AdminBugReportPage page = await reader.GetPageAsync(Filter(), CancellationToken.None);
        Assert.Multiple(() => Assert.False(page.IsConfigured), () => Assert.Empty(page.Items), () => Assert.Equal(0, page.TotalItems));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Configured_EncodesFiltersAddsReadKeyAndDeserializes(bool populated) {
        var expected = new AdminBugReportPage([new AdminBugReportEntry(Guid.NewGuid(), Guid.NewGuid(), DateTimeOffset.UnixEpoch, "subject", "processed", 2, Summary: null, MergeRequestUrl: null, ContentExpired: true)], 31);
        using var handler = new ResponseHandler(() => new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(expected) });
        using var client = new HttpClient(handler) { BaseAddress = new Uri("https://bugs.example.com/") };
        var reader = new AdminBugReportReader(client, Options.Create(new AdminBugTriageOptions { BaseUrl = client.BaseAddress.ToString(), ReadApiKey = new string('k', 32) }));
        AdminBugReportFilter filter = populated ? Filter() : new AdminBugReportFilter(1, 50, FromUtc: null, ToUtc: null, Status: null, Search: null, Id: null);
        AdminBugReportPage actual = await reader.GetPageAsync(filter, CancellationToken.None);
        Assert.Equivalent(expected, actual, strict: true);
        Assert.Equal(new string('k', 32), handler.ReadKey);
        Assert.NotNull(handler.Uri);
        Assert.Equal("/api/report-journal", handler.Uri.AbsolutePath);
        Assert.Contains(populated ? "search=subject%20%26%20details" : "search=", handler.Uri.Query, StringComparison.Ordinal);
        Assert.Contains(populated ? "page=2&limit=10" : "page=1&limit=50", handler.Uri.Query, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(HttpStatusCode.BadGateway)]
    [InlineData(HttpStatusCode.Unauthorized)]
    public async Task HttpFailure_IsNotConvertedIntoEmptyPage(HttpStatusCode status) {
        using var handler = new ResponseHandler(() => new HttpResponseMessage(status));
        using var client = new HttpClient(handler) { BaseAddress = new Uri("https://bugs.example.com/") };
        var reader = new AdminBugReportReader(client, Options.Create(new AdminBugTriageOptions { BaseUrl = client.BaseAddress.ToString(), ReadApiKey = new string('k', 32) }));
        HttpRequestException error = await Assert.ThrowsAsync<HttpRequestException>(() => reader.GetPageAsync(Filter(), CancellationToken.None));
        Assert.Equal(status, error.StatusCode);
    }

    [Fact]
    public async Task NullResponse_ThrowsInsteadOfReportingEmptyJournal() {
        using var handler = new ResponseHandler(() => new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("null", System.Text.Encoding.UTF8, "application/json") });
        using var client = new HttpClient(handler) { BaseAddress = new Uri("https://bugs.example.com/") };
        var reader = new AdminBugReportReader(client, Options.Create(new AdminBugTriageOptions { BaseUrl = client.BaseAddress.ToString(), ReadApiKey = new string('k', 32) }));
        await Assert.ThrowsAsync<InvalidOperationException>(() => reader.GetPageAsync(Filter(), CancellationToken.None));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Registration_ResolvesConfiguredAndDisabledReaders(bool configured) {
        var values = new Dictionary<string, string?>(StringComparer.Ordinal) {
            ["AdminBugTriage:BaseUrl"] = configured ? "https://bugs.example.com/api/" : "",
            ["AdminBugTriage:ReadApiKey"] = configured ? new string('k', 32) : "",
        };
        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(values).Build();
        var services = new ServiceCollection();
        Assert.Same(services, services.AddAdminBugTriageIntegration(configuration));
        using ServiceProvider provider = services.BuildServiceProvider();
        Assert.IsType<AdminBugReportReader>(provider.GetRequiredService<IAdminBugReportReader>());
    }

    private static AdminBugReportFilter Filter() => new(2, 10, DateTimeOffset.UnixEpoch, DateTimeOffset.UnixEpoch.AddDays(1), "processed", "subject & details", Guid.NewGuid());

    [ExcludeFromCodeCoverage]
    private sealed class ResponseHandler(Func<HttpResponseMessage> response) : HttpMessageHandler {
        public Uri? Uri { get; private set; }
        public string? ReadKey { get; private set; }
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) {
            Uri = request.RequestUri;
            ReadKey = request.Headers.GetValues("X-BugTriage-Read-Key").Single();
            return Task.FromResult(response());
        }
    }
}
