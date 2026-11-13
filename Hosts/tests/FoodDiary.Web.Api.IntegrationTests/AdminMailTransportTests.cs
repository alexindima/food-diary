using System.Net;
using System.Text.Json;
using FoodDiary.Web.Api.IntegrationTests.TestInfrastructure;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Web.Api.IntegrationTests;

[ExcludeFromCodeCoverage]
public sealed class AdminMailTransportTests(TransportApiWebApplicationFactory factory)
    : IClassFixture<TransportApiWebApplicationFactory> {
    [Theory]
    [InlineData("/api/v1/admin/outgoing-emails", "Admin.OutgoingMailUnavailable")]
    [InlineData("/api/v1/admin/mail-inbox/messages/page", "Admin.MailInboxUnavailable")]
    [InlineData("/api/v1/admin/mail-inbox/messages/page?recipient=feedback%40fooddiary.club", "Admin.MailInboxUnavailable")]
    public async Task MailPage_ReportsDependencyFailureAndRecoversOnRetryAsync(string path, string errorCode) {
        var upstream = new MailEndpoint();
        await using WebApplicationFactory<Program> configured = factory.WithWebHostBuilder(builder =>
            builder.ConfigureTestServices(services => {
                services.AddHttpClient("IMailInboxClient")
                    .ConfigurePrimaryHttpMessageHandler(() => upstream.CreateHandler());
                services.AddHttpClient("IMailRelayJournalClient")
                    .ConfigurePrimaryHttpMessageHandler(() => upstream.CreateHandler());
            }));
        using HttpClient client = configured.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthenticationHandler.AuthenticateHeader, "true");
        client.DefaultRequestHeaders.Add(TestAuthenticationHandler.UserIdHeader, Guid.NewGuid().ToString());
        client.DefaultRequestHeaders.Add(TestAuthenticationHandler.RoleHeader, "Admin");

        using HttpResponseMessage failed = await client.GetAsync(path);
        Assert.Equal(HttpStatusCode.BadGateway, failed.StatusCode);
        using var error = JsonDocument.Parse(await failed.Content.ReadAsStringAsync());
        Assert.Equal(errorCode, error.RootElement.GetProperty("error").GetString());
        Assert.Equal("A required dependency could not complete the request.", error.RootElement.GetProperty("message").GetString());

        upstream.IsAvailable = true;
        using HttpResponseMessage recovered = await client.GetAsync(path);
        Assert.Equal(HttpStatusCode.OK, recovered.StatusCode);
        using var page = JsonDocument.Parse(await recovered.Content.ReadAsStringAsync());
        Assert.Equal(0, page.RootElement.GetProperty("totalItems").GetInt64());
        if (path.Contains("recipient=", StringComparison.Ordinal)) {
            Assert.Contains("recipient=feedback%40fooddiary.club", upstream.LastQuery, StringComparison.Ordinal);
        }
    }

    [ExcludeFromCodeCoverage]
    private sealed class MailEndpoint {
        public bool IsAvailable { get; set; }
        public string LastQuery { get; private set; } = string.Empty;

        public HttpMessageHandler CreateHandler() => new Handler(this);

        [ExcludeFromCodeCoverage]
        private sealed class Handler(MailEndpoint endpoint) : HttpMessageHandler {
            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) {
                endpoint.LastQuery = request.RequestUri!.Query;
                return Task.FromResult(endpoint.IsAvailable
                    ? new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(new { items = Array.Empty<object>(), totalItems = 0 }) }
                    : new HttpResponseMessage(HttpStatusCode.NotImplemented));
            }
        }
    }
}
