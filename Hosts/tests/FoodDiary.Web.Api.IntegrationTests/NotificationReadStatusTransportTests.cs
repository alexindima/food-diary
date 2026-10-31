using System.Net;
using System.Net.Http.Json;
using FoodDiary.Web.Api.IntegrationTests.TestInfrastructure;

namespace FoodDiary.Web.Api.IntegrationTests;

[ExcludeFromCodeCoverage]
public sealed class NotificationReadStatusTransportTests(TransportApiWebApplicationFactory factory)
    : IClassFixture<TransportApiWebApplicationFactory> {
    [Fact]
    public async Task ReadStatus_RequiresAuthenticationAndBoundsRequestAsync() {
        using HttpClient anonymous = factory.CreateClient();
        using HttpResponseMessage unauthorized = await anonymous.PostAsJsonAsync("/api/v1/notifications/read-status", new { notificationIds = new[] { Guid.NewGuid() } });
        Assert.Equal(HttpStatusCode.Unauthorized, unauthorized.StatusCode);

        using HttpClient authenticated = factory.CreateClient();
        authenticated.DefaultRequestHeaders.Add(TestAuthenticationHandler.AuthenticateHeader, "true");
        authenticated.DefaultRequestHeaders.Add(TestAuthenticationHandler.UserIdHeader, Guid.NewGuid().ToString());
        foreach (Guid[] ids in new Guid[][] { [], [Guid.Empty], [.. Enumerable.Range(0, 101).Select(_ => Guid.NewGuid())] }) {
            using HttpResponseMessage invalid = await authenticated.PostAsJsonAsync("/api/v1/notifications/read-status", new { notificationIds = ids });
            Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);
        }
        using HttpResponseMessage missingIds = await authenticated.PostAsJsonAsync("/api/v1/notifications/read-status", new { });
        Assert.Equal(HttpStatusCode.BadRequest, missingIds.StatusCode);
    }
}
