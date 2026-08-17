using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FoodDiary.Presentation.Api.Responses;
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

        ApiErrorHttpResponse? error = await response.Content.ReadFromJsonAsync<ApiErrorHttpResponse>();
        Assert.NotNull(error);
        Assert.Multiple(
            () => Assert.Equal(HttpStatusCode.RequestEntityTooLarge, response.StatusCode),
            () => Assert.Equal("Request.PayloadTooLarge", error.Error),
            () => Assert.Equal("The request payload is too large.", error.Message),
            () => Assert.False(string.IsNullOrWhiteSpace(error.TraceId)));
    }
}
