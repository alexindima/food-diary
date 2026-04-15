using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FoodDiary.Presentation.Api.Features.Auth.Requests;
using FoodDiary.Presentation.Api.Features.Dietologist.Requests;
using FoodDiary.Web.Api.IntegrationTests.TestInfrastructure;

namespace FoodDiary.Web.Api.IntegrationTests;

public sealed class PostgresDietologistInvitationNotificationIntegrationTests(PostgresApiWebApplicationFactory factory)
    : IClassFixture<PostgresApiWebApplicationFactory> {
    private static readonly JsonSerializerOptions JsonOptions = new() {
        PropertyNameCaseInsensitive = true,
    };

    [RequiresDockerFact]
    public async Task DietologistInvitationNotifications_CanBeMarkedReadInBulk() {
        var firstClient = await CreateAuthenticatedClientAsync("client-a");
        var secondClient = await CreateAuthenticatedClientAsync("client-b");
        var dietologistUser = await CreateAuthenticatedClientAsync("dietologist");

        await InviteDietologistAsync(firstClient.Client, dietologistUser.Email);
        await InviteDietologistAsync(secondClient.Client, dietologistUser.Email);

        var initialNotifications = await GetNotificationsAsync(dietologistUser.Client);
        var invitationNotifications = initialNotifications.Where(x => x.Type == "DietologistInvitationReceived").ToList();

        Assert.Equal(2, invitationNotifications.Count);
        Assert.All(invitationNotifications, notification => Assert.False(notification.IsRead));

        var readAllResponse = await dietologistUser.Client.PutAsJsonAsync("/api/v1/notifications/read-all", new { });
        await AssertStatusCodeAsync(HttpStatusCode.NoContent, readAllResponse);

        var afterReadAll = await GetNotificationsAsync(dietologistUser.Client);
        var invitationNotificationsAfterReadAll = afterReadAll.Where(x => x.Type == "DietologistInvitationReceived").ToList();

        Assert.Equal(2, invitationNotificationsAfterReadAll.Count);
        Assert.All(invitationNotificationsAfterReadAll, notification => Assert.True(notification.IsRead));
    }

    private async Task<AuthenticatedUser> CreateAuthenticatedClientAsync(string emailPrefix = "api-tests") {
        var client = factory.CreateClient();
        var email = $"{emailPrefix}-{Guid.NewGuid():N}@example.com";
        var registerResponse = await client.PostAsJsonAsync(
            "/api/v1/auth/register",
            new RegisterHttpRequest(email, "Password123!", "en"));
        registerResponse.EnsureSuccessStatusCode();

        var authPayload = await registerResponse.Content.ReadFromJsonAsync<AuthPayload>(JsonOptions);
        Assert.NotNull(authPayload);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authPayload.AccessToken);

        return new AuthenticatedUser(client, email);
    }

    private async Task InviteDietologistAsync(HttpClient client, string dietologistEmail) {
        var inviteResponse = await client.PostAsJsonAsync(
            "/api/v1/dietologist/invite",
            new InviteDietologistHttpRequest(
                dietologistEmail,
                new DietologistPermissionsHttpRequest()));
        await AssertStatusCodeAsync(HttpStatusCode.NoContent, inviteResponse);
    }

    private async Task<List<NotificationPayload>> GetNotificationsAsync(HttpClient client) {
        var notificationsResponse = await client.GetAsync("/api/v1/notifications");
        await AssertStatusCodeAsync(HttpStatusCode.OK, notificationsResponse);
        var notifications = await notificationsResponse.Content.ReadFromJsonAsync<List<NotificationPayload>>(JsonOptions);
        Assert.NotNull(notifications);
        return notifications;
    }

    private static async Task AssertStatusCodeAsync(HttpStatusCode expected, HttpResponseMessage response) {
        if (response.StatusCode == expected) {
            return;
        }

        var body = await response.Content.ReadAsStringAsync();
        Assert.Fail($"Expected {(int)expected} ({expected}), got {(int)response.StatusCode} ({response.StatusCode}). Body: {body}");
    }

    private sealed record AuthPayload(string AccessToken);

    private sealed record AuthenticatedUser(HttpClient Client, string Email);

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
