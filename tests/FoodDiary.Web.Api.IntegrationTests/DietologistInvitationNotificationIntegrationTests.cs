using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FoodDiary.Presentation.Api.Features.Auth.Requests;
using FoodDiary.Presentation.Api.Features.Dietologist.Requests;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.Enums;
using FoodDiary.Infrastructure.Persistence;
using FoodDiary.Web.Api.IntegrationTests.TestInfrastructure;
using FoodDiary.Application.Abstractions.Authentication.Abstractions;
using FoodDiary.Domain.ValueObjects.Ids;
using Microsoft.Extensions.DependencyInjection;
using System.Globalization;

namespace FoodDiary.Web.Api.IntegrationTests;

[ExcludeFromCodeCoverage]
public sealed class DietologistInvitationNotificationIntegrationTests(ApiWebApplicationFactory factory)
    : IClassFixture<ApiWebApplicationFactory> {
    private static readonly JsonSerializerOptions JsonOptions = new() {
        PropertyNameCaseInsensitive = true,
    };

    [Fact]
    public async Task InviteRegisteredDietologist_CreatesInAppNotification() {
        AuthenticatedUser clientUser = await CreateAuthenticatedClientAsync();
        AuthenticatedUser dietologistUser = await CreateAuthenticatedClientAsync("dietologist");

        HttpResponseMessage inviteResponse = await clientUser.Client.PostAsJsonAsync(
            "/api/v1/dietologist/invite",
            new InviteDietologistHttpRequest(
                dietologistUser.Email,
                new DietologistPermissionsHttpRequest()));
        await AssertStatusCodeAsync(HttpStatusCode.NoContent, inviteResponse);

        HttpResponseMessage notificationsResponse = await dietologistUser.Client.GetAsync("/api/v1/notifications");
        await AssertStatusCodeAsync(HttpStatusCode.OK, notificationsResponse);
        List<NotificationPayload>? notifications = await notificationsResponse.Content.ReadFromJsonAsync<List<NotificationPayload>>(JsonOptions);

        Assert.NotNull(notifications);
        NotificationPayload invitationNotification = Assert.Single(notifications, x => string.Equals(x.Type, "DietologistInvitationReceived", StringComparison.Ordinal));
        Assert.Equal("DietologistInvitationReceived", invitationNotification.Type);
        Assert.NotNull(invitationNotification.ReferenceId);
        Assert.Equal($"/dietologist-invitations/{invitationNotification.ReferenceId}", invitationNotification.TargetUrl);
        Assert.False(invitationNotification.IsRead);
        Assert.False(string.IsNullOrWhiteSpace(invitationNotification.Title));
    }

    [Fact]
    public async Task DietologistInvitationNotification_CanBeMarkedRead_Individually() {
        AuthenticatedUser firstClient = await CreateAuthenticatedClientAsync("client-a");
        AuthenticatedUser dietologistUser = await CreateAuthenticatedClientAsync("dietologist");

        await InviteDietologistAsync(firstClient.Client, dietologistUser.Email);

        List<NotificationPayload> initialNotifications = await GetNotificationsAsync(dietologistUser.Client);
        NotificationPayload firstNotification = Assert.Single(initialNotifications, x => string.Equals(x.Type, "DietologistInvitationReceived", StringComparison.Ordinal));
        Assert.False(firstNotification.IsRead);

        HttpResponseMessage markReadResponse = await dietologistUser.Client.PutAsJsonAsync(
            $"/api/v1/notifications/{firstNotification.Id}/read",
            new { });
        await AssertStatusCodeAsync(HttpStatusCode.NoContent, markReadResponse);

        List<NotificationPayload> afterSingleRead = await GetNotificationsAsync(dietologistUser.Client);
        NotificationPayload readNotification = Assert.Single(afterSingleRead, x => x.Id == firstNotification.Id);
        Assert.True(readNotification.IsRead);
    }

    [Fact]
    public async Task AcceptCurrentUser_CreatesClientNotification() {
        AuthenticatedUser clientUser = await CreateAuthenticatedClientAsync("client");
        AuthenticatedUser dietologistUser = await CreateAuthenticatedClientAsync("dietologist");
        await EnsureDietologistRoleAsync();

        await InviteDietologistAsync(clientUser.Client, dietologistUser.Email);
        DietologistRelationshipPayload relationship = await GetRelationshipAsync(clientUser.Client);

        HttpResponseMessage acceptResponse = await dietologistUser.Client.PostAsJsonAsync(
            $"/api/v1/dietologist/invitations/{relationship.InvitationId}/accept-current-user",
            new { });
        await AssertStatusCodeAsync(HttpStatusCode.NoContent, acceptResponse);

        List<NotificationPayload> notifications = await GetNotificationsAsync(clientUser.Client);
        NotificationPayload notification = Assert.Single(notifications, x => string.Equals(x.Type, "DietologistInvitationAccepted", StringComparison.Ordinal));
        Assert.Equal("DietologistInvitationAccepted", notification.Type);
        Assert.Equal(relationship.InvitationId.ToString(), notification.ReferenceId);
        Assert.Equal("/profile", notification.TargetUrl);
        Assert.False(notification.IsRead);
        Assert.False(string.IsNullOrWhiteSpace(notification.Title));
    }

    [Fact]
    public async Task DeclineCurrentUser_CreatesClientNotification() {
        AuthenticatedUser clientUser = await CreateAuthenticatedClientAsync("client");
        AuthenticatedUser dietologistUser = await CreateAuthenticatedClientAsync("dietologist");

        await InviteDietologistAsync(clientUser.Client, dietologistUser.Email);
        DietologistRelationshipPayload relationship = await GetRelationshipAsync(clientUser.Client);

        HttpResponseMessage declineResponse = await dietologistUser.Client.PostAsJsonAsync(
            $"/api/v1/dietologist/invitations/{relationship.InvitationId}/decline-current-user",
            new { });
        await AssertStatusCodeAsync(HttpStatusCode.NoContent, declineResponse);

        List<NotificationPayload> notifications = await GetNotificationsAsync(clientUser.Client);
        NotificationPayload notification = Assert.Single(notifications, x => string.Equals(x.Type, "DietologistInvitationDeclined", StringComparison.Ordinal));
        Assert.Equal("DietologistInvitationDeclined", notification.Type);
        Assert.Equal(relationship.InvitationId.ToString(), notification.ReferenceId);
        Assert.Equal("/profile", notification.TargetUrl);
        Assert.False(notification.IsRead);
        Assert.False(string.IsNullOrWhiteSpace(notification.Title));
    }

    [Fact]
    public async Task CreateRecommendation_CreatesClientNotification_WithTargetUrl_AndCanBeMarkedRead() {
        AuthenticatedUser clientUser = await CreateAuthenticatedClientAsync("client");
        AuthenticatedUser dietologistUser = await CreateAuthenticatedClientAsync("dietologist");

        await InviteDietologistAsync(clientUser.Client, dietologistUser.Email);
        DietologistRelationshipPayload relationship = await GetRelationshipAsync(clientUser.Client);

        HttpResponseMessage acceptResponse = await dietologistUser.Client.PostAsJsonAsync(
            $"/api/v1/dietologist/invitations/{relationship.InvitationId}/accept-current-user",
            new { });
        await AssertStatusCodeAsync(HttpStatusCode.NoContent, acceptResponse);
        SetDietologistToken(dietologistUser);

        HttpResponseMessage recommendationResponse = await dietologistUser.Client.PostAsJsonAsync(
            $"/api/v1/dietologist/clients/{clientUser.UserId}/recommendations",
            new CreateRecommendationHttpRequest("Add more protein at breakfast."));
        await AssertStatusCodeAsync(HttpStatusCode.Created, recommendationResponse);
        RecommendationPayload? recommendation = await recommendationResponse.Content.ReadFromJsonAsync<RecommendationPayload>(JsonOptions);
        Assert.NotNull(recommendation);

        List<NotificationPayload> notifications = await GetNotificationsAsync(clientUser.Client);
        NotificationPayload notification = Assert.Single(notifications, x => string.Equals(x.Type, "NewRecommendation", StringComparison.Ordinal));
        Assert.Equal(recommendation.Id.ToString(), notification.ReferenceId);
        Assert.Equal($"/recommendations?recommendationId={recommendation.Id}", notification.TargetUrl);
        Assert.False(notification.IsRead);
        Assert.Contains(dietologistUser.Email, notification.Title, StringComparison.OrdinalIgnoreCase);

        HttpResponseMessage markReadResponse = await clientUser.Client.PutAsJsonAsync(
            $"/api/v1/recommendations/{recommendation.Id}/read",
            new { });
        await AssertStatusCodeAsync(HttpStatusCode.NoContent, markReadResponse);

        HttpResponseMessage recommendationsResponse = await clientUser.Client.GetAsync("/api/v1/recommendations");
        await AssertStatusCodeAsync(HttpStatusCode.OK, recommendationsResponse);
        List<RecommendationPayload>? recommendations = await recommendationsResponse.Content.ReadFromJsonAsync<List<RecommendationPayload>>(JsonOptions);
        Assert.NotNull(recommendations);
        RecommendationPayload readRecommendation = Assert.Single(recommendations, x => x.Id == recommendation.Id);
        Assert.True(readRecommendation.IsRead);
    }

    private async Task InviteDietologistAsync(HttpClient client, string dietologistEmail) {
        HttpResponseMessage inviteResponse = await client.PostAsJsonAsync(
            "/api/v1/dietologist/invite",
            new InviteDietologistHttpRequest(
                dietologistEmail,
                new DietologistPermissionsHttpRequest())).ConfigureAwait(false);
        await AssertStatusCodeAsync(HttpStatusCode.NoContent, inviteResponse).ConfigureAwait(false);
    }

    private async Task EnsureDietologistRoleAsync() {
        using IServiceScope scope = factory.Services.CreateScope();
        FoodDiaryDbContext dbContext = scope.ServiceProvider.GetRequiredService<FoodDiaryDbContext>();
        if (!dbContext.Roles.Any(role => role.Name == RoleNames.Dietologist)) {
            dbContext.Roles.Add(Role.Create(RoleNames.Dietologist));
            await dbContext.SaveChangesAsync().ConfigureAwait(false);
        }
    }

    private async Task<List<NotificationPayload>> GetNotificationsAsync(HttpClient client) {
        HttpResponseMessage notificationsResponse = await client.GetAsync("/api/v1/notifications").ConfigureAwait(false);
        await AssertStatusCodeAsync(HttpStatusCode.OK, notificationsResponse).ConfigureAwait(false);
        List<NotificationPayload>? notifications = await notificationsResponse.Content.ReadFromJsonAsync<List<NotificationPayload>>(JsonOptions).ConfigureAwait(false);
        Assert.NotNull(notifications);
        return notifications;
    }

    private async Task<DietologistRelationshipPayload> GetRelationshipAsync(HttpClient client) {
        HttpResponseMessage relationshipResponse = await client.GetAsync("/api/v1/dietologist/relationship").ConfigureAwait(false);
        await AssertStatusCodeAsync(HttpStatusCode.OK, relationshipResponse).ConfigureAwait(false);
        DietologistRelationshipPayload? relationship = await relationshipResponse.Content.ReadFromJsonAsync<DietologistRelationshipPayload>(JsonOptions).ConfigureAwait(false);
        Assert.NotNull(relationship);
        return relationship;
    }

    private void SetDietologistToken(AuthenticatedUser user) {
        using IServiceScope scope = factory.Services.CreateScope();
        IJwtTokenGenerator tokenGenerator = scope.ServiceProvider.GetRequiredService<IJwtTokenGenerator>();
        string token = tokenGenerator.GenerateAccessToken(new UserId(user.UserId), user.Email, [RoleNames.Dietologist]);
        user.Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
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

        Assert.NotEqual(Guid.Empty, authPayload.User.Id);
        return new AuthenticatedUser(client, email, authPayload.User.Id);
    }

    private static async Task AssertStatusCodeAsync(HttpStatusCode expected, HttpResponseMessage response) {
        if (response.StatusCode == expected) {
            return;
        }

        string body = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
        Assert.Fail(string.Create(CultureInfo.InvariantCulture, $"Expected {(int)expected} ({expected}), got {(int)response.StatusCode} ({response.StatusCode}). Body: {body}"));
    }

    [ExcludeFromCodeCoverage]
    private sealed record AuthPayload(string AccessToken, AuthUserPayload User);

    [ExcludeFromCodeCoverage]
    private sealed record AuthUserPayload(Guid Id);

    [ExcludeFromCodeCoverage]
    private sealed record AuthenticatedUser(HttpClient Client, string Email, Guid UserId);

    [ExcludeFromCodeCoverage]
    private sealed record RecommendationPayload(
        Guid Id,
        Guid DietologistUserId,
        string? DietologistFirstName,
        string? DietologistLastName,
        string Text,
        bool IsRead,
        DateTime CreatedAtUtc,
        DateTime? ReadAtUtc);

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

    [ExcludeFromCodeCoverage]
    private sealed record DietologistRelationshipPayload(
        Guid InvitationId,
        string Status,
        string Email,
        string? FirstName,
        string? LastName,
        Guid? DietologistUserId,
        object Permissions,
        DateTime CreatedAtUtc,
        DateTime ExpiresAtUtc,
        DateTime? AcceptedAtUtc);
}
