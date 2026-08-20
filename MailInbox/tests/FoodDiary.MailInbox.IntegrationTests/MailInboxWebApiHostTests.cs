using System.Net;
using FoodDiary.MailInbox.IntegrationTests.TestInfrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace FoodDiary.MailInbox.IntegrationTests;

[ExcludeFromCodeCoverage]
public sealed class MailInboxWebApiHostTests(MailInboxWebApiFactory factory)
    : IClassFixture<MailInboxWebApiFactory> {
    [Theory]
    [InlineData("/health")]
    [InlineData("/health/ready")]
    public async Task HealthEndpoint_WhenHostStarts_ReturnsOk(string path) {
        HttpClient client = factory.CreateClient();

        HttpResponseMessage response = await client.GetAsync(path);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task ProtectedEndpoint_WithoutApiKey_ReturnsUnauthorized() {
        HttpClient client = factory.CreateClient();

        HttpResponseMessage response = await client.GetAsync("/api/mail-inbox/messages");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public void ProductionHost_WithPlaceholderDatabasePassword_FailsStartup() {
        using WebApplicationFactory<global::Program> productionFactory = factory.WithWebHostBuilder(builder => {
            builder.UseEnvironment(Environments.Production);
            builder.ConfigureAppConfiguration((_, configuration) => {
                configuration.AddInMemoryCollection(new Dictionary<string, string?>(StringComparer.Ordinal) {
                    ["ConnectionStrings:DefaultConnection"] =
                        "Host=database;Database=mailbox;Username=postgres;Password=change-me-local-password",
                });
            });
        });

        OptionsValidationException exception = Assert.Throws<OptionsValidationException>(productionFactory.CreateClient);

        Assert.Contains("does not use the repository-local database password", exception.Message, StringComparison.Ordinal);
    }
}
