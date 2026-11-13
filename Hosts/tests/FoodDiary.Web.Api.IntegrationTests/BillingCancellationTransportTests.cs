using System.Net;
using FoodDiary.Web.Api.IntegrationTests.TestInfrastructure;

namespace FoodDiary.Web.Api.IntegrationTests;

[ExcludeFromCodeCoverage]
public sealed class BillingCancellationTransportTests(TransportApiWebApplicationFactory factory)
    : IClassFixture<TransportApiWebApplicationFactory> {
    [Theory]
    [InlineData("/api/v1/billing/cancel")]
    [InlineData("/api/v1/billing/cancellations/11111111-1111-1111-1111-111111111111/feedback")]
    public async Task AnonymousCancellationAndFeedback_AreRejectedBeforeDispatch(string path) {
        using HttpClient client = factory.CreateClient();
        using HttpResponseMessage response = await client.PostAsJsonAsync(path, new { });
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task OversizedOptionalFeedback_ReturnsValidationErrorBeforeHandlerDispatch() {
        using HttpClient client = factory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthenticationHandler.AuthenticateHeader, "true");
        client.DefaultRequestHeaders.Add(TestAuthenticationHandler.UserIdHeader, Guid.NewGuid().ToString());
        using HttpResponseMessage response = await client.PostAsJsonAsync(
            "/api/v1/billing/cancellations/11111111-1111-1111-1111-111111111111/feedback",
            new { reasonCode = "other", comment = new string('a', 2001) });
        Assert.True(response.StatusCode == HttpStatusCode.BadRequest, "Oversized optional feedback must return 400 before handler dispatch.");
    }

    [Fact]
    public async Task CancellationsReport_RequiresAdminRole() {
        using HttpClient client = factory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthenticationHandler.AuthenticateHeader, "true");
        client.DefaultRequestHeaders.Add(TestAuthenticationHandler.UserIdHeader, Guid.NewGuid().ToString());
        client.DefaultRequestHeaders.Add(TestAuthenticationHandler.RoleHeader, "User");
        using HttpResponseMessage response = await client.GetAsync("/api/v1/admin/billing/cancellations");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Theory]
    [InlineData("/api/v1/billing/cancel")]
    [InlineData("/api/v1/billing/cancellations/11111111-1111-1111-1111-111111111111/feedback")]
    public async Task Impersonation_CannotCancelOrSubmitFeedback(string path) {
        using HttpClient client = factory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthenticationHandler.AuthenticateHeader, "true");
        client.DefaultRequestHeaders.Add(TestAuthenticationHandler.UserIdHeader, Guid.NewGuid().ToString());
        client.DefaultRequestHeaders.Add(TestAuthenticationHandler.ImpersonationHeader, "true");
        client.DefaultRequestHeaders.Add(TestAuthenticationHandler.ImpersonationActorUserIdHeader, Guid.NewGuid().ToString());
        using HttpResponseMessage response = await client.PostAsJsonAsync(path, new { });
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
