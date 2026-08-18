using System.Net;
using FoodDiary.Web.Api.IntegrationTests.TestInfrastructure;

namespace FoodDiary.Web.Api.IntegrationTests;

[ExcludeFromCodeCoverage]
public sealed class SwaggerUiIntegrationTests(ApiWebApplicationFactory apiFactory)
    : IClassFixture<ApiWebApplicationFactory> {
    [Fact]
    public async Task SwaggerUi_InDevelopment_LoadsWithCompatibleContentSecurityPolicy() {
        HttpClient client = apiFactory.CreateClient();

        HttpResponseMessage indexResponse = await client.GetAsync("/swagger/index.html");
        HttpResponseMessage stylesheetResponse = await client.GetAsync("/swagger/swagger-ui.css");

        Assert.Multiple(
            () => Assert.Equal(HttpStatusCode.OK, indexResponse.StatusCode),
            () => Assert.Equal(HttpStatusCode.OK, stylesheetResponse.StatusCode));
        Assert.True(indexResponse.Headers.TryGetValues("Content-Security-Policy", out IEnumerable<string>? values));
        string policy = Assert.Single(values);
        Assert.Multiple(
            () => Assert.Contains("script-src 'self' 'unsafe-inline'", policy, StringComparison.Ordinal),
            () => Assert.Contains("style-src 'self' 'unsafe-inline'", policy, StringComparison.Ordinal),
            () => Assert.Contains("connect-src 'self'", policy, StringComparison.Ordinal),
            () => Assert.Contains("frame-ancestors 'none'", policy, StringComparison.Ordinal));
    }
}
