using FoodDiary.Application.Common.Interfaces.Persistence;
using FoodDiary.Application.Notifications.Common;
using FoodDiary.Domain.Entities.Notifications;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.ValueObjects;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Integrations.Services;
using Microsoft.Extensions.Logging.Abstractions;
using WebPushOptions = FoodDiary.Integrations.Options.WebPushOptions;

namespace FoodDiary.Infrastructure.Tests.Services;

public sealed class WebPushNotificationSenderTests {
    [Fact]
    public async Task SendAsync_WhenMasterPushDisabled_DoesNotLoadSubscriptions() {
        var user = User.Create("user@example.com", "hash");
        var subscriptionRepository = new RecordingSubscriptionRepository();
        var sender = CreateSender(subscriptionRepository, new SingleUserRepository(user));
        var notification = Notification.Create(user.Id, NotificationTypes.FastingCompleted, "{}");

        await sender.SendAsync(notification, CancellationToken.None);

        Assert.Equal(0, subscriptionRepository.GetByUserCalls);
    }

    [Fact]
    public async Task SendAsync_WhenCategoryDisabled_DoesNotLoadSubscriptions() {
        var user = User.Create("user@example.com", "hash");
        user.UpdatePreferences(new UserPreferenceUpdate(
            PushNotificationsEnabled: true,
            FastingPushNotificationsEnabled: false));
        var subscriptionRepository = new RecordingSubscriptionRepository();
        var sender = CreateSender(subscriptionRepository, new SingleUserRepository(user));
        var notification = Notification.Create(user.Id, NotificationTypes.FastingCompleted, "{}");

        await sender.SendAsync(notification, CancellationToken.None);

        Assert.Equal(0, subscriptionRepository.GetByUserCalls);
    }

    [Fact]
    public async Task SendAsync_WhenCategoryEnabled_LoadsSubscriptions() {
        var user = User.Create("user@example.com", "hash");
        user.UpdatePreferences(new UserPreferenceUpdate(
            PushNotificationsEnabled: true,
            SocialPushNotificationsEnabled: true));
        var subscriptionRepository = new RecordingSubscriptionRepository();
        var sender = CreateSender(subscriptionRepository, new SingleUserRepository(user));
        var notification = Notification.Create(user.Id, NotificationTypes.NewComment, "{}");

        await sender.SendAsync(notification, CancellationToken.None);

        Assert.Equal(1, subscriptionRepository.GetByUserCalls);
    }

    [Fact]
    public async Task SendAsync_WhenSubscriptionExpired_PrunesItBeforeSending() {
        var user = User.Create("user@example.com", "hash");
        user.UpdatePreferences(new UserPreferenceUpdate(
            PushNotificationsEnabled: true,
            FastingPushNotificationsEnabled: true));
        var expiredSubscription = WebPushSubscription.Create(
            user.Id,
            "https://push.example.com/subscriptions/expired",
            "p256",
            "auth",
            DateTime.UtcNow.AddMinutes(-5),
            "en",
            "Chrome");
        var subscriptionRepository = new RecordingSubscriptionRepository([expiredSubscription]);
        var sender = CreateSender(subscriptionRepository, new SingleUserRepository(user));
        var notification = Notification.Create(user.Id, NotificationTypes.FastingCompleted, "{}");

        await sender.SendAsync(notification, CancellationToken.None);

        Assert.Equal(1, subscriptionRepository.GetByUserCalls);
        Assert.Single(subscriptionRepository.DeletedSubscriptions);
        Assert.Equal(expiredSubscription.Endpoint, subscriptionRepository.DeletedSubscriptions[0].Endpoint);
    }

    private static WebPushNotificationSender CreateSender(
        RecordingSubscriptionRepository subscriptionRepository,
        IUserRepository userRepository) {
        return new WebPushNotificationSender(
            subscriptionRepository,
            userRepository,
            new StubNotificationTextRenderer(),
            Microsoft.Extensions.Options.Options.Create(new WebPushOptions {
                Enabled = true,
                Subject = "https://example.com",
                PublicKey = "public",
                PrivateKey = "private",
                DefaultUrl = "/"
            }),
            NullLogger<WebPushNotificationSender>.Instance);
    }

    private sealed class RecordingSubscriptionRepository(IEnumerable<WebPushSubscription>? subscriptions = null) : IWebPushSubscriptionRepository {
        public int GetByUserCalls { get; private set; }
        public List<WebPushSubscription> DeletedSubscriptions { get; } = [];

        public Task<WebPushSubscription?> GetByEndpointAsync(string endpoint, bool asTracking = false, CancellationToken cancellationToken = default) =>
            Task.FromResult<WebPushSubscription?>(null);

        public Task<IReadOnlyList<WebPushSubscription>> GetByUserAsync(UserId userId, CancellationToken cancellationToken = default) {
            GetByUserCalls++;
            return Task.FromResult<IReadOnlyList<WebPushSubscription>>(subscriptions?.ToList() ?? []);
        }

        public Task<WebPushSubscription> AddAsync(WebPushSubscription subscription, CancellationToken cancellationToken = default) =>
            Task.FromResult(subscription);

        public Task UpdateAsync(WebPushSubscription subscription, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task DeleteAsync(WebPushSubscription subscription, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task DeleteRangeAsync(IReadOnlyCollection<WebPushSubscription> subscriptions, CancellationToken cancellationToken = default) {
            DeletedSubscriptions.AddRange(subscriptions);
            return Task.CompletedTask;
        }
    }

    private sealed class SingleUserRepository(User user) : IUserRepository {
        public Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<User?> GetByEmailIncludingDeletedAsync(string email, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<User?> GetByIdAsync(UserId id, CancellationToken cancellationToken = default) => Task.FromResult<User?>(user.Id == id ? user : null);
        public Task<User?> GetByIdIncludingDeletedAsync(UserId id, CancellationToken cancellationToken = default) => Task.FromResult<User?>(user.Id == id ? user : null);
        public Task<User?> GetByTelegramUserIdAsync(long telegramUserId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<User?> GetByTelegramUserIdIncludingDeletedAsync(long telegramUserId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<(IReadOnlyList<User> Items, int TotalItems)> GetPagedAsync(string? search, int page, int limit, bool includeDeleted, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<(int TotalUsers, int ActiveUsers, int PremiumUsers, int DeletedUsers, IReadOnlyList<User> RecentUsers)> GetAdminDashboardSummaryAsync(int recentLimit, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<IReadOnlyList<Role>> GetRolesByNamesAsync(IReadOnlyList<string> names, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<User> AddAsync(User userToAdd, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task UpdateAsync(User userToUpdate, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class StubNotificationTextRenderer : INotificationTextRenderer {
        public NotificationText Render(string type, string? locale = null, params object[] arguments) =>
            new("Title", "Body");

        public NotificationText RenderFromPayload(string type, string payloadJson, string? locale = null) =>
            new("Title", "Body");
    }
}
