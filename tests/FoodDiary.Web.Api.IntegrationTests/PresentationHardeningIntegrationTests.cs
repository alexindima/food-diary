using System.Net;
using System.Net.Http.Json;
using FoodDiary.Presentation.Api.Features.Exercises.Requests;
using FoodDiary.Presentation.Api.Features.Notifications.Requests;
using FoodDiary.Web.Api.IntegrationTests.TestInfrastructure;

namespace FoodDiary.Web.Api.IntegrationTests;

[ExcludeFromCodeCoverage]
public sealed class PresentationHardeningIntegrationTests(TestAuthApiWebApplicationFactory factory)
    : IClassFixture<TestAuthApiWebApplicationFactory> {
    [Theory]
    [InlineData("POST", "/api/v1/dietologist/invite")]
    [InlineData("DELETE", "/api/v1/dietologist/relationship")]
    [InlineData("PUT", "/api/v1/dietologist/permissions")]
    [InlineData("POST", "/api/v1/dietologist/accept")]
    [InlineData("POST", "/api/v1/dietologist/decline")]
    [InlineData("POST", "/api/v1/dietologist/invitations/11111111-1111-1111-1111-111111111111/accept-current-user")]
    [InlineData("POST", "/api/v1/dietologist/invitations/11111111-1111-1111-1111-111111111111/decline-current-user")]
    [InlineData("POST", "/api/v1/cycles")]
    [InlineData("PUT", "/api/v1/cycles/11111111-1111-1111-1111-111111111111/consents/1")]
    public async Task SensitiveMutations_WithImpersonatedUser_ReturnForbidden(string method, string route) {
        HttpClient client = CreateAuthenticatedClient(impersonated: true);
        using var request = new HttpRequestMessage(new HttpMethod(method), route) {
            Content = JsonContent.Create(new { }),
        };

        HttpResponseMessage response = await client.SendAsync(request);
        ErrorPayload? error = await response.Content.ReadFromJsonAsync<ErrorPayload>();

        Assert.Multiple(
            () => Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode),
            () => Assert.Equal("Authentication.ImpersonationActionForbidden", Assert.IsType<ErrorPayload>(error).Error));
    }

    [Theory]
    [InlineData("POST", "/api/v1/exercises")]
    [InlineData("PUT", "/api/v1/exercises/11111111-1111-1111-1111-111111111111")]
    public async Task ExerciseMutation_WithDurationOverDomainLimit_ReturnsBadRequest(string method, string route) {
        HttpClient client = CreateAuthenticatedClient();
        object payload = string.Equals(method, "POST", StringComparison.Ordinal)
            ? new CreateExerciseEntryHttpRequest(DateTime.UtcNow, "Running", 1441, 100, Name: null, Notes: null)
            : new UpdateExerciseEntryHttpRequest(
                ExerciseType: null,
                DurationMinutes: 1441,
                CaloriesBurned: null,
                Name: null,
                ClearName: false,
                Notes: null,
                ClearNotes: false,
                Date: null);
        using var request = new HttpRequestMessage(new HttpMethod(method), route) {
            Content = JsonContent.Create(payload),
        };

        HttpResponseMessage response = await client.SendAsync(request);
        ErrorPayload? error = await response.Content.ReadFromJsonAsync<ErrorPayload>();

        Assert.Multiple(
            () => Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode),
            () => Assert.Equal("Validation.Invalid", Assert.IsType<ErrorPayload>(error).Error));
    }

    [Fact]
    public async Task UpsertWebPushSubscription_WithUnspecifiedExpiration_ReturnsBadRequest() {
        HttpClient client = CreateAuthenticatedClient();
        var request = new UpsertWebPushSubscriptionHttpRequest(
            "https://push.example.com/subscription",
            new DateTime(2026, 8, 19, 12, 0, 0, DateTimeKind.Unspecified),
            new UpsertWebPushSubscriptionKeysHttpRequest("p256", "auth"),
            "en",
            "Browser");

        HttpResponseMessage response = await client.PutAsJsonAsync(
            "/api/v1/notifications/push/subscription",
            request);
        ErrorPayload? error = await response.Content.ReadFromJsonAsync<ErrorPayload>();

        Assert.Multiple(
            () => Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode),
            () => Assert.Equal("Validation.Invalid", Assert.IsType<ErrorPayload>(error).Error));
    }

    private HttpClient CreateAuthenticatedClient(bool impersonated = false) {
        HttpClient client = factory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthenticationHandler.AuthenticateHeader, "true");
        client.DefaultRequestHeaders.Add(TestAuthenticationHandler.UserIdHeader, Guid.NewGuid().ToString());
        if (impersonated) {
            client.DefaultRequestHeaders.Add(TestAuthenticationHandler.ImpersonationHeader, "true");
            client.DefaultRequestHeaders.Add(
                TestAuthenticationHandler.ImpersonationActorUserIdHeader,
                Guid.NewGuid().ToString());
        }

        return client;
    }

    [ExcludeFromCodeCoverage]
    private sealed record ErrorPayload(string Error, string Message, string TraceId);
}
