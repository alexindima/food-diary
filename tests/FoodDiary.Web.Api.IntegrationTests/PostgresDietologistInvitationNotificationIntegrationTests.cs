using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FoodDiary.Presentation.Api.Features.Auth.Requests;
using FoodDiary.Presentation.Api.Features.Dietologist.Requests;
using FoodDiary.Web.Api.IntegrationTests.TestInfrastructure;

namespace FoodDiary.Web.Api.IntegrationTests;

[ExcludeFromCodeCoverage]
public sealed class PostgresDietologistInvitationNotificationIntegrationTests(PostgresApiWebApplicationFactory factory)
    : IClassFixture<PostgresApiWebApplicationFactory> {
    private static readonly JsonSerializerOptions JsonOptions = new() {
        PropertyNameCaseInsensitive = true,
    };

    [RequiresDockerFact]
    public async Task DietologistInvitationNotifications_CanBeMarkedReadInBulk() {
        AuthenticatedUser firstClient = await CreateAuthenticatedClientAsync("client-a");
        AuthenticatedUser secondClient = await CreateAuthenticatedClientAsync("client-b");
        AuthenticatedUser dietologistUser = await CreateAuthenticatedClientAsync("dietologist");

        await InviteDietologistAsync(firstClient.Client, dietologistUser.Email);
        await InviteDietologistAsync(secondClient.Client, dietologistUser.Email);

        List<NotificationPayload> initialNotifications = await GetNotificationsAsync(dietologistUser.Client);
        var invitationNotifications = initialNotifications.Where(x => string.Equals(x.Type, "DietologistInvitationReceived", StringComparison.Ordinal)).ToList();

        Assert.Equal(2, invitationNotifications.Count);
        Assert.All(invitationNotifications, notification => Assert.False(notification.IsRead));

        HttpResponseMessage readAllResponse = await dietologistUser.Client.PutAsJsonAsync("/api/v1/notifications/read-all", new { });
        await AssertStatusCodeAsync(HttpStatusCode.NoContent, readAllResponse);

        List<NotificationPayload> afterReadAll = await GetNotificationsAsync(dietologistUser.Client);
        var invitationNotificationsAfterReadAll = afterReadAll.Where(x => string.Equals(x.Type, "DietologistInvitationReceived", StringComparison.Ordinal)).ToList();

        Assert.Equal(2, invitationNotificationsAfterReadAll.Count);
        Assert.All(invitationNotificationsAfterReadAll, notification => Assert.True(notification.IsRead));
    }

    private async Task<AuthenticatedUser> CreateAuthenticatedClientAsync(string emailPrefix = "api-tests") {
        HttpClient client = factory.CreateClient();
        string email = $"{emailPrefix}-{Guid.NewGuid():N}@example.com";
        HttpResponseMessage registerResponse = await client.PostAsJsonAsync(
            "/api/v1/auth/register",
            new RegisterHttpRequest(email, "Password123!", "en")).ConfigureAwait(false);
        registerResponse.EnsureSuccessStatusCode();

        AuthPayload? authPayload = await registerResponse.Content.ReadFromJsonAsync<AuthPayload>(JsonOptions).ConfigureAwait(false);
        Assert.NotNull(authPayload);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authPayload.AccessToken);

        return new AuthenticatedUser(client, email);
    }

    private async Task InviteDietologistAsync(HttpClient client, string dietologistEmail) {
        HttpResponseMessage inviteResponse = await client.PostAsJsonAsync(
            "/api/v1/dietologist/invite",
            new InviteDietologistHttpRequest(
                dietologistEmail,
                new DietologistPermissionsHttpRequest())).ConfigureAwait(false);
        await AssertStatusCodeAsync(HttpStatusCode.NoContent, inviteResponse).ConfigureAwait(false);
    }

    private async Task<List<NotificationPayload>> GetNotificationsAsync(HttpClient client) {
        HttpResponseMessage notificationsResponse = await client.GetAsync("/api/v1/notifications").ConfigureAwait(false);
        await AssertStatusCodeAsync(HttpStatusCode.OK, notificationsResponse).ConfigureAwait(false);
        List<NotificationPayload>? notifications = await notificationsResponse.Content.ReadFromJsonAsync<List<NotificationPayload>>(JsonOptions).ConfigureAwait(false);
        Assert.NotNull(notifications);
        return notifications;
    }

    private static async Task AssertStatusCodeAsync(HttpStatusCode expected, HttpResponseMessage response) {
        if (response.StatusCode == expected) {
            return;
        }

        string body = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
        Assert.Fail($"Expected {(int)expected} ({expected}), got {(int)response.StatusCode} ({response.StatusCode}). Body: {body}");
    }

    [ExcludeFromCodeCoverage]
    private sealed record AuthPayload(string AccessToken);

    [ExcludeFromCodeCoverage]
    private sealed record AuthenticatedUser(HttpClient Client, string Email);

    [ExcludeFromCodeCoverage]
    private sealed record NotificationPayload(
        Guid Id,
        string Type,
        string Title,
        string? Body,
        string? TargetUrl,
        string? ReferenceId,
        bool IsRead,
        DateTime CreatedAtUtc);
}
