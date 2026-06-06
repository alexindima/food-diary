using System.Net;
using System.Net.Http.Json;
using FoodDiary.Domain.Entities.Notifications;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Infrastructure.Persistence;
using FoodDiary.Web.Api.IntegrationTests.TestInfrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Web.Api.IntegrationTests;

[ExcludeFromCodeCoverage]
public sealed class NotificationPreferencesIntegrationTests(TestAuthApiWebApplicationFactory factory)
    : IClassFixture<TestAuthApiWebApplicationFactory> {
    [Fact]
    public async Task GetAndUpdateNotificationPreferences_UsesDedicatedNotificationsEndpoint() {
        User user = await SeedUserAsync();
        HttpClient client = factory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthenticationHandler.AuthenticateHeader, "true");
        client.DefaultRequestHeaders.Add(TestAuthenticationHandler.UserIdHeader, user.Id.Value.ToString());

        HttpResponseMessage getResponse = await client.GetAsync("/api/v1/notifications/preferences");
        NotificationPreferencesPayload? initialPreferences = await getResponse.Content.ReadFromJsonAsync<NotificationPreferencesPayload>();

        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
        Assert.NotNull(initialPreferences);
        Assert.False(initialPreferences.PushNotificationsEnabled);
        Assert.True(initialPreferences.FastingPushNotificationsEnabled);
        Assert.True(initialPreferences.SocialPushNotificationsEnabled);

        HttpResponseMessage updateResponse = await client.PutAsJsonAsync(
            "/api/v1/notifications/preferences",
            new UpdateNotificationPreferencesPayload(
                PushNotificationsEnabled: true,
                FastingPushNotificationsEnabled: false,
                SocialPushNotificationsEnabled: true));
        NotificationPreferencesPayload? updatedPreferences = await updateResponse.Content.ReadFromJsonAsync<NotificationPreferencesPayload>();

        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);
        Assert.NotNull(updatedPreferences);
        Assert.True(updatedPreferences.PushNotificationsEnabled);
        Assert.False(updatedPreferences.FastingPushNotificationsEnabled);
        Assert.True(updatedPreferences.SocialPushNotificationsEnabled);

        await using AsyncServiceScope scope = factory.Services.CreateAsyncScope();
        FoodDiaryDbContext dbContext = scope.ServiceProvider.GetRequiredService<FoodDiaryDbContext>();
        User? persistedUser = await dbContext.Users.FindAsync(user.Id);
        Assert.NotNull(persistedUser);
        Assert.True(persistedUser.PushNotificationsEnabled);
        Assert.False(persistedUser.FastingPushNotificationsEnabled);
        Assert.True(persistedUser.SocialPushNotificationsEnabled);
    }

    [Fact]
    public async Task GetAndRemoveWebPushSubscriptions_ReturnsActiveDevicesOnly() {
        User user = await SeedUserAsync();
        await SeedSubscriptionAsync(
            user,
            endpoint: "https://push.example.com/subscriptions/current",
            expirationTimeUtc: DateTime.UtcNow.AddDays(2));
        await SeedSubscriptionAsync(
            user,
            endpoint: "https://push.example.com/subscriptions/expired",
            expirationTimeUtc: DateTime.UtcNow.AddMinutes(-10));

        HttpClient client = factory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthenticationHandler.AuthenticateHeader, "true");
        client.DefaultRequestHeaders.Add(TestAuthenticationHandler.UserIdHeader, user.Id.Value.ToString());

        HttpResponseMessage listResponse = await client.GetAsync("/api/v1/notifications/push/subscriptions");
        List<WebPushSubscriptionPayload>? subscriptions = await listResponse.Content.ReadFromJsonAsync<List<WebPushSubscriptionPayload>>();

        Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);
        Assert.NotNull(subscriptions);
        WebPushSubscriptionPayload subscription = Assert.Single(subscriptions);
        Assert.Equal("https://push.example.com/subscriptions/current", subscription.Endpoint);
        Assert.Equal("push.example.com", subscription.EndpointHost);

        HttpResponseMessage deleteResponse = await client.SendAsync(new HttpRequestMessage(HttpMethod.Delete, "/api/v1/notifications/push/subscription") {
            Content = JsonContent.Create(new RemoveWebPushSubscriptionPayload(subscription.Endpoint))
        });

        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        HttpResponseMessage listAfterDeleteResponse = await client.GetAsync("/api/v1/notifications/push/subscriptions");
        List<WebPushSubscriptionPayload>? subscriptionsAfterDelete = await listAfterDeleteResponse.Content.ReadFromJsonAsync<List<WebPushSubscriptionPayload>>();

        Assert.Equal(HttpStatusCode.OK, listAfterDeleteResponse.StatusCode);
        Assert.NotNull(subscriptionsAfterDelete);
        Assert.Empty(subscriptionsAfterDelete);
    }

    private async Task<User> SeedUserAsync() {
        AsyncServiceScope scope = factory.Services.CreateAsyncScope();
        await using (scope.ConfigureAwait(false)) {
            FoodDiaryDbContext dbContext = scope.ServiceProvider.GetRequiredService<FoodDiaryDbContext>();
            var user = User.Create($"notifications-{Guid.NewGuid():N}@example.com", "hash");
            dbContext.Users.Add(user);
            await dbContext.SaveChangesAsync().ConfigureAwait(false);
            return user;
        }
    }

    private async Task SeedSubscriptionAsync(User user, string endpoint, DateTime? expirationTimeUtc) {
        AsyncServiceScope scope = factory.Services.CreateAsyncScope();
        await using (scope.ConfigureAwait(false)) {
            FoodDiaryDbContext dbContext = scope.ServiceProvider.GetRequiredService<FoodDiaryDbContext>();
            dbContext.WebPushSubscriptions.Add(WebPushSubscription.Create(
                user.Id,
                endpoint,
                "p256",
                "auth",
                expirationTimeUtc,
                "en",
                "Chrome"));
            await dbContext.SaveChangesAsync().ConfigureAwait(false);
        }
    }

    [ExcludeFromCodeCoverage]
    private sealed record NotificationPreferencesPayload(
        bool PushNotificationsEnabled,
        bool FastingPushNotificationsEnabled,
        bool SocialPushNotificationsEnabled);

    [ExcludeFromCodeCoverage]
    private sealed record WebPushSubscriptionPayload(
        string Endpoint,
        string EndpointHost,
        DateTime? ExpirationTimeUtc,
        string? Locale,
        string? UserAgent,
        DateTime CreatedAtUtc,
        DateTime? UpdatedAtUtc);

    [ExcludeFromCodeCoverage]
    private sealed record RemoveWebPushSubscriptionPayload(string Endpoint);

    [ExcludeFromCodeCoverage]
    private sealed record UpdateNotificationPreferencesPayload(
        bool? PushNotificationsEnabled,
        bool? FastingPushNotificationsEnabled,
        bool? SocialPushNotificationsEnabled);
}
