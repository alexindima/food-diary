using System.Net;
using System.Net.Http.Json;
using FoodDiary.Domain.Primitives;
using FoodDiary.Infrastructure.Persistence;
using FoodDiary.Modules.Notifications.Domain.Entities;
using FoodDiary.Modules.Users.Domain.Entities;
using FoodDiary.Web.Api.IntegrationTests.TestInfrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Web.Api.IntegrationTests;

[ExcludeFromCodeCoverage]
public sealed class NotificationReadStatusIntegrationTests(TestAuthApiWebApplicationFactory factory)
    : IClassFixture<TestAuthApiWebApplicationFactory> {
    [RequiresDockerFact]
    public async Task ReadStatus_SynchronizesDevicesBeyondFeedLimitAndPreservesOwnershipAsync() {
        await using AsyncServiceScope scope = factory.Services.CreateAsyncScope();
        FoodDiaryDbContext context = scope.ServiceProvider.GetRequiredService<FoodDiaryDbContext>();
        var user = User.Create($"push-sync-{Guid.NewGuid():N}@example.com", "hash");
        var other = User.Create($"push-sync-other-{Guid.NewGuid():N}@example.com", "hash");
        context.Users.AddRange(user, other);
        Notification old;
        using (DomainTime.Override(new FixedTimeProvider(DateTimeOffset.UtcNow.AddDays(-1)))) {
            old = Notification.Create(user.Id, "info", "{}");
        }
        var unread = Notification.Create(user.Id, "info", "{}");
        var foreign = Notification.Create(other.Id, "info", "{}");
        foreign.MarkAsRead();
        context.Notifications.AddRange(old, unread, foreign);
        for (int index = 0; index < 60; index++) {
            context.Notifications.Add(Notification.Create(user.Id, "info", "{}"));
        }
        await context.SaveChangesAsync();

        using HttpClient desktop = CreateClient(user.Id.Value);
        using HttpClient phone = CreateClient(user.Id.Value);
        Guid[] ids = [old.Id.Value, unread.Id.Value, foreign.Id.Value, Guid.NewGuid()];
        using HttpResponseMessage initial = await phone.PostAsJsonAsync("/api/v1/notifications/read-status", new { notificationIds = ids });
        initial.EnsureSuccessStatusCode();
        Assert.Empty((await initial.Content.ReadFromJsonAsync<ReadStatusPayload>())!.ReadIds);

        using HttpResponseMessage read = await desktop.PutAsync($"/api/v1/notifications/{old.Id.Value}/read", content: null);
        Assert.Equal(HttpStatusCode.NoContent, read.StatusCode);
        using HttpResponseMessage synchronized = await phone.PostAsJsonAsync("/api/v1/notifications/read-status", new { notificationIds = ids });
        synchronized.EnsureSuccessStatusCode();
        Assert.Equal(old.Id.Value, Assert.Single((await synchronized.Content.ReadFromJsonAsync<ReadStatusPayload>())!.ReadIds));

        using HttpResponseMessage readAll = await desktop.PutAsync("/api/v1/notifications/read-all", content: null);
        Assert.Equal(HttpStatusCode.NoContent, readAll.StatusCode);
        using HttpResponseMessage all = await phone.PostAsJsonAsync("/api/v1/notifications/read-status", new { notificationIds = ids });
        all.EnsureSuccessStatusCode();
        Assert.Equal(new[] { old.Id.Value, unread.Id.Value }.Order(), (await all.Content.ReadFromJsonAsync<ReadStatusPayload>())!.ReadIds.Order());
    }

    private HttpClient CreateClient(Guid userId) {
        HttpClient client = factory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthenticationHandler.AuthenticateHeader, "true");
        client.DefaultRequestHeaders.Add(TestAuthenticationHandler.UserIdHeader, userId.ToString());
        return client;
    }

    [ExcludeFromCodeCoverage]
    private sealed record ReadStatusPayload(Guid[] ReadIds);

    [ExcludeFromCodeCoverage]
    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
