using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FoodDiary.Presentation.Api.Features.Auth.Requests;
using FoodDiary.Presentation.Api.Features.Dietologist.Requests;
using FoodDiary.Web.Api.IntegrationTests.TestInfrastructure;

namespace FoodDiary.Web.Api.IntegrationTests;

public sealed class DietologistInvitationNotificationIntegrationTests(ApiWebApplicationFactory factory)
    : IClassFixture<ApiWebApplicationFactory> {
    private static readonly JsonSerializerOptions JsonOptions = new() {
        PropertyNameCaseInsensitive = true,
    };

    [Fact]
    public async Task InviteRegisteredDietologist_CreatesInAppNotification() {
        var clientUser = await CreateAuthenticatedClientAsync();
        var dietologistUser = await CreateAuthenticatedClientAsync("dietologist");

        var inviteResponse = await clientUser.Client.PostAsJsonAsync(
            "/api/v1/dietologist/invite",
            new InviteDietologistHttpRequest(
                dietologistUser.Email,
                new DietologistPermissionsHttpRequest()));
        await AssertStatusCodeAsync(HttpStatusCode.NoContent, inviteResponse);

        var notificationsResponse = await dietologistUser.Client.GetAsync("/api/v1/notifications");
        await AssertStatusCodeAsync(HttpStatusCode.OK, notificationsResponse);
        var notifications = await notificationsResponse.Content.ReadFromJsonAsync<List<NotificationPayload>>(JsonOptions);

        Assert.NotNull(notifications);
        var invitationNotification = Assert.Single(notifications, x => x.Type == "DietologistInvitationReceived");
        Assert.Equal("DietologistInvitationReceived", invitationNotification.Type);
        Assert.NotNull(invitationNotification.ReferenceId);
        Assert.Equal($"/dietologist-invitations/{invitationNotification.ReferenceId}", invitationNotification.TargetUrl);
        Assert.False(invitationNotification.IsRead);
        Assert.False(string.IsNullOrWhiteSpace(invitationNotification.Title));
    }

    [Fact]
    public async Task DietologistInvitationNotification_CanBeMarkedRead_Individually() {
        var firstClient = await CreateAuthenticatedClientAsync("client-a");
        var dietologistUser = await CreateAuthenticatedClientAsync("dietologist");

        await InviteDietologistAsync(firstClient.Client, dietologistUser.Email);

        var initialNotifications = await GetNotificationsAsync(dietologistUser.Client);
        var firstNotification = Assert.Single(initialNotifications, x => x.Type == "DietologistInvitationReceived");
        Assert.False(firstNotification.IsRead);

        var markReadResponse = await dietologistUser.Client.PutAsJsonAsync(
            $"/api/v1/notifications/{firstNotification.Id}/read",
            new { });
        await AssertStatusCodeAsync(HttpStatusCode.NoContent, markReadResponse);

        var afterSingleRead = await GetNotificationsAsync(dietologistUser.Client);
        var readNotification = Assert.Single(afterSingleRead, x => x.Id == firstNotification.Id);
        Assert.True(readNotification.IsRead);
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
