using System.Net;
using System.Net.Http.Headers;
using FoodDiary.Web.Api.IntegrationTests.TestInfrastructure;

namespace FoodDiary.Web.Api.IntegrationTests;

[ExcludeFromCodeCoverage]
public sealed class BillingSecurityIntegrationTests(ApiWebApplicationFactory factory) : IClassFixture<ApiWebApplicationFactory> {
    [Fact]
    public async Task BillingWebhook_WhenPayloadExceedsProviderLimit_ReturnsPayloadTooLarge() {
        HttpClient client = factory.CreateClient();
        using var content = new ByteArrayContent(new byte[(64 * 1024) + 1]);
        content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

        HttpResponseMessage response = await client.PostAsync(
            "/api/v1/billing/webhooks/stripe",
            content);

        Assert.Equal(HttpStatusCode.RequestEntityTooLarge, response.StatusCode);
    }
}
