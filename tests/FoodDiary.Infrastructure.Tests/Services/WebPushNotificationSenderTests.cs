using System.Globalization;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Application.Abstractions.Users.Models;
using FoodDiary.Application.Abstractions.Notifications.Common;
using FoodDiary.Application.Abstractions.Notifications.Models;
using FoodDiary.Application.Notifications.Services;
using FoodDiary.Domain.Entities.Notifications;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.ValueObjects;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Integrations.Services;
using FoodDiary.Results;
using Microsoft.Extensions.Logging.Abstractions;
using WebPush;
using WebPushOptions = FoodDiary.Integrations.Options.WebPushOptions;

namespace FoodDiary.Infrastructure.Tests.Services;

[ExcludeFromCodeCoverage]
public sealed class WebPushNotificationSenderTests {
    [Theory]
    [InlineData(true, "public", true)]
    [InlineData(true, "", false)]
    [InlineData(false, "public", false)]
    public void GetClientConfiguration_ReturnsEnabledOnlyWhenConfigured(bool enabled, string publicKey, bool expectedEnabled) {
        var subscriptionRepository = new RecordingSubscriptionRepository();
        WebPushNotificationSender sender = CreateSender(
            subscriptionRepository,
            new NullUserRepository(),
            new WebPushOptions {
                Enabled = enabled,
                Subject = "https://example.com",
                PublicKey = publicKey,
                PrivateKey = "private",
                DefaultUrl = "/",
            });

        WebPushClientConfiguration configuration = sender.GetClientConfiguration();

        Assert.Equal(expectedEnabled, configuration.Enabled);
        Assert.Equal(publicKey, configuration.PublicKey);
    }

    [Fact]
    public async Task SendAsync_WhenMasterPushDisabled_DoesNotLoadSubscriptions() {
        var user = User.Create("user@example.com", "hash");
        var subscriptionRepository = new RecordingSubscriptionRepository();
        WebPushNotificationSender sender = CreateSender(subscriptionRepository, new SingleUserRepository(user));
        var notification = Notification.Create(user.Id, NotificationTypes.FastingCompleted, "{}");

        await sender.SendAsync(notification, CancellationToken.None);

        Assert.Equal(0, subscriptionRepository.GetByUserCalls);
    }

    [Fact]
    public async Task SendAsync_WhenWebPushConfigurationDisabled_DoesNotLoadUser() {
        var user = User.Create("user@example.com", "hash");
        var userRepository = new RecordingUserRepository(user);
        var subscriptionRepository = new RecordingSubscriptionRepository();
        WebPushNotificationSender sender = CreateSender(
            subscriptionRepository,
            userRepository,
            new WebPushOptions {
                Enabled = false,
                Subject = "https://example.com",
                PublicKey = "public",
                PrivateKey = "private",
                DefaultUrl = "/",
            });
        var notification = Notification.Create(user.Id, NotificationTypes.FastingCompleted, "{}");

        await sender.SendAsync(notification, CancellationToken.None);

        Assert.Equal(0, userRepository.GetByIdCalls);
        Assert.Equal(0, subscriptionRepository.GetByUserCalls);
    }

    [Fact]
    public async Task SendAsync_WhenCategoryDisabled_DoesNotLoadSubscriptions() {
        var user = User.Create("user@example.com", "hash");
        user.UpdatePreferences(new UserPreferenceUpdate(
            PushNotificationsEnabled: true,
            FastingPushNotificationsEnabled: false));
        var subscriptionRepository = new RecordingSubscriptionRepository();
        WebPushNotificationSender sender = CreateSender(subscriptionRepository, new SingleUserRepository(user));
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
        WebPushNotificationSender sender = CreateSender(subscriptionRepository, new SingleUserRepository(user));
        var notification = Notification.Create(user.Id, NotificationTypes.NewComment, "{}");

        await sender.SendAsync(notification, CancellationToken.None);

        Assert.Equal(1, subscriptionRepository.GetByUserCalls);
    }

    [Fact]
    public async Task SendAsync_WhenAudienceExceedsLimit_SendsOnlyMaximumAllowedSubscriptions() {
        var user = User.Create("bounded-push@example.com", "hash");
        user.UpdatePreferences(new UserPreferenceUpdate(
            PushNotificationsEnabled: true,
            FastingPushNotificationsEnabled: true));
        WebPushSubscription[] subscriptions = [.. Enumerable
            .Range(0, WebPushDeliveryLimits.MaximumSubscriptionsPerUser + 2)
            .Select(index => {
                string suffix = index.ToString(CultureInfo.InvariantCulture);
                return WebPushSubscription.Create(
                    user.Id,
                    $"https://push.example.com/subscriptions/{suffix}",
                    $"p256-{suffix}",
                    $"auth-{suffix}",
                    FixedNow.AddHours(1));
            })];
        var repository = new RecordingSubscriptionRepository(subscriptions);
        var webPushClient = new StubWebPushClientAdapter();
        var sender = new WebPushNotificationSender(
            CreateAudienceService(repository, new SingleUserRepository(user)),
            new StubNotificationTextRenderer(),
            Microsoft.Extensions.Options.Options.Create(new WebPushOptions {
                Enabled = true,
                Subject = "https://example.com",
                PublicKey = "public",
                PrivateKey = "private",
                DefaultUrl = "/",
            }),
            webPushClient,
            FixedTime,
            NullLogger<WebPushNotificationSender>.Instance);

        await sender.SendAsync(
            Notification.Create(user.Id, NotificationTypes.FastingCompleted, "{}"),
            CancellationToken.None);

        Assert.Equal(WebPushDeliveryLimits.MaximumSubscriptionsPerUser, webPushClient.SendCalls);
    }

    [Fact]
    public async Task SendAsync_WhenUserMissing_DoesNotLoadSubscriptions() {
        var userId = UserId.New();
        var subscriptionRepository = new RecordingSubscriptionRepository();
        WebPushNotificationSender sender = CreateSender(subscriptionRepository, new NullUserRepository());
        var notification = Notification.Create(userId, NotificationTypes.FastingCompleted, "{}");

        await sender.SendAsync(notification, CancellationToken.None);

        Assert.Equal(0, subscriptionRepository.GetByUserCalls);
    }

    [Fact]
    public async Task SendAsync_WhenCancellationIsRequested_PropagatesCancellation() {
        var user = User.Create("user@example.com", "hash");
        user.UpdatePreferences(new UserPreferenceUpdate(
            PushNotificationsEnabled: true,
            FastingPushNotificationsEnabled: true));
        var activeSubscription = WebPushSubscription.Create(
            user.Id,
            "https://push.example.com/subscriptions/active",
            "p256",
            "auth",
            FixedNow.AddMinutes(30),
            "en",
            "Chrome");
        var subscriptionRepository = new RecordingSubscriptionRepository([activeSubscription]);
        WebPushNotificationSender sender = CreateSender(subscriptionRepository, new SingleUserRepository(user));
        var notification = Notification.Create(user.Id, NotificationTypes.FastingCompleted, "{}");
        using var cancellationTokenSource = new CancellationTokenSource();
        await cancellationTokenSource.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            sender.SendAsync(notification, cancellationTokenSource.Token));

        Assert.Equal(1, subscriptionRepository.GetByUserCalls);
        Assert.Empty(subscriptionRepository.DeletedSubscriptions);
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
            FixedNow.AddMinutes(-5),
            "en",
            "Chrome");
        var subscriptionRepository = new RecordingSubscriptionRepository([expiredSubscription]);
        WebPushNotificationSender sender = CreateSender(subscriptionRepository, new SingleUserRepository(user));
        var notification = Notification.Create(user.Id, NotificationTypes.FastingCompleted, "{}");

        await sender.SendAsync(notification, CancellationToken.None);

        Assert.Equal(1, subscriptionRepository.GetByUserCalls);
        Assert.Single(subscriptionRepository.DeletedSubscriptions);
        Assert.Equal(expiredSubscription.Endpoint, subscriptionRepository.DeletedSubscriptions[0].Endpoint);
    }

    [Fact]
    public void BuildPayload_UsesResolvedAbsoluteTargetUrl() {
        var user = User.Create("user@example.com", "hash");
        WebPushNotificationSender sender = CreateSender(
            new RecordingSubscriptionRepository(),
            new SingleUserRepository(user),
            new WebPushOptions {
                Enabled = true,
                Subject = "https://example.com",
                PublicKey = "public",
                PrivateKey = "private",
                DefaultUrl = "https://example.com/",
            });
        var notification = Notification.Create(
            user.Id,
            NotificationTypes.NewRecommendation,
            "{}",
            "11111111-1111-1111-1111-111111111111");

        string payload = InvokePrivate<string>(
            sender,
            "BuildPayload",
            notification,
            new NotificationText("Comment", "New comment"));

        Assert.Contains("Comment", payload, StringComparison.Ordinal);
        Assert.Contains("New comment", payload, StringComparison.Ordinal);
        Assert.Contains($"\"tag\":\"{notification.Id.Value:N}\"", payload, StringComparison.Ordinal);
        Assert.Contains("https://example.com", payload, StringComparison.Ordinal);
        Assert.Contains(NotificationTypes.NewRecommendation, payload, StringComparison.Ordinal);
        Assert.Contains("11111111-1111-1111-1111-111111111111", payload, StringComparison.Ordinal);
    }

    [Fact]
    public void ResolveUrl_WhenDefaultUrlIsRelative_ReturnsRelativePath() {
        var user = User.Create("user@example.com", "hash");
        WebPushNotificationSender sender = CreateSender(
            new RecordingSubscriptionRepository(),
            new SingleUserRepository(user),
            new WebPushOptions {
                Enabled = true,
                Subject = "https://example.com",
                PublicKey = "public",
                PrivateKey = "private",
                DefaultUrl = "/fallback",
            });
        var notification = Notification.Create(user.Id, "unknown", "{}");

        string url = InvokePrivate<string>(sender, "ResolveUrl", notification);

        Assert.Equal("/fallback", url);
    }

    [Theory]
    [InlineData(System.Net.HttpStatusCode.Gone, true)]
    [InlineData(System.Net.HttpStatusCode.NotFound, true)]
    [InlineData(System.Net.HttpStatusCode.BadRequest, false)]
    public void IsExpiredSubscription_ReturnsTrueOnlyForGoneOrNotFound(System.Net.HttpStatusCode statusCode, bool expected) {
        var exception = new WebPushException(
            "failed",
            new PushSubscription("https://push.example.com/subscriptions/1", "p256", "auth"),
            new HttpResponseMessage(statusCode));

        bool result = InvokePrivateStatic<bool>("IsExpiredSubscription", exception);

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(NotificationTypes.EatingWindowStarted, true, false, true)]
    [InlineData(NotificationTypes.FastingWindowStarted, true, false, true)]
    [InlineData(NotificationTypes.FastingCheckInReminder, true, false, true)]
    [InlineData(NotificationTypes.DietologistInvitationReceived, false, true, true)]
    [InlineData(NotificationTypes.DietologistInvitationAccepted, false, true, true)]
    [InlineData(NotificationTypes.DietologistInvitationDeclined, false, true, true)]
    [InlineData(NotificationTypes.NewRecommendation, false, true, true)]
    [InlineData(NotificationTypes.NewComment, false, true, true)]
    [InlineData("unknown", false, false, true)]
    [InlineData(NotificationTypes.EatingWindowStarted, false, false, false)]
    [InlineData(NotificationTypes.DietologistInvitationReceived, false, false, false)]
    public void IsCategoryEnabled_MapsNotificationTypesToUserPreferences(
        string notificationType,
        bool fastingEnabled,
        bool socialEnabled,
        bool expected) {
        var user = User.Create("user@example.com", "hash");
        user.UpdatePreferences(new UserPreferenceUpdate(
            PushNotificationsEnabled: true,
            FastingPushNotificationsEnabled: fastingEnabled,
            SocialPushNotificationsEnabled: socialEnabled));

        bool result = InvokeAudiencePrivateStatic<bool>("IsCategoryEnabled", ToNotificationProfile(user), notificationType);

        Assert.Equal(expected, result);
    }

    private static WebPushNotificationSender CreateSender(
        RecordingSubscriptionRepository subscriptionRepository,
        IUserRepository userRepository,
        WebPushOptions? options = null) {
        return new WebPushNotificationSender(
            CreateAudienceService(subscriptionRepository, userRepository),
            new StubNotificationTextRenderer(),
            Microsoft.Extensions.Options.Options.Create(options ?? new WebPushOptions {
                Enabled = true,
                Subject = "https://example.com",
                PublicKey = "public",
                PrivateKey = "private",
                DefaultUrl = "/",
            }),
            new StubWebPushClientAdapter(),
            FixedTime,
            NullLogger<WebPushNotificationSender>.Instance);
    }

    [Fact]
    public async Task SendAsync_WhenPushClientReportsExpiredSubscription_DeletesInvalidSubscriptions() {
        var user = User.Create("expired@example.com", "hash");
        user.UpdatePreferences(new UserPreferenceUpdate(
            PushNotificationsEnabled: true,
            FastingPushNotificationsEnabled: true));
        var activeSubscription = WebPushSubscription.Create(
            user.Id,
            "https://push.example.com/subscriptions/expired",
            "p256",
            "auth",
            FixedNow.AddMinutes(30),
            "en",
            "Chrome");
        var subscriptionRepository = new RecordingSubscriptionRepository([activeSubscription]);
        var webPushClient = new StubWebPushClientAdapter(new WebPushException(
            "gone",
            new PushSubscription(activeSubscription.Endpoint, activeSubscription.P256Dh, activeSubscription.Auth),
            new HttpResponseMessage(System.Net.HttpStatusCode.Gone)));
        var sender = new WebPushNotificationSender(
            CreateAudienceService(subscriptionRepository, new SingleUserRepository(user)),
            new StubNotificationTextRenderer(),
            Microsoft.Extensions.Options.Options.Create(new WebPushOptions {
                Enabled = true,
                Subject = "https://example.com",
                PublicKey = "public",
                PrivateKey = "private",
                DefaultUrl = "/",
            }),
            webPushClient,
            FixedTime,
            NullLogger<WebPushNotificationSender>.Instance);
        var notification = Notification.Create(user.Id, NotificationTypes.FastingCompleted, "{}");

        await sender.SendAsync(notification, CancellationToken.None);

        Assert.Equal(1, webPushClient.SendCalls);
        WebPushSubscription deleted = Assert.Single(subscriptionRepository.DeletedSubscriptions);
        Assert.Equal(activeSubscription.Id, deleted.Id);
    }

    [Fact]
    public async Task SendAsync_WhenPushClientSucceeds_DoesNotDeleteSubscriptions() {
        var user = User.Create("delivered@example.com", "hash");
        user.UpdatePreferences(new UserPreferenceUpdate(
            PushNotificationsEnabled: true,
            FastingPushNotificationsEnabled: true));
        var activeSubscription = WebPushSubscription.Create(
            user.Id,
            "https://push.example.com/subscriptions/active",
            "p256",
            "auth",
            FixedNow.AddMinutes(30),
            "en",
            "Chrome");
        var subscriptionRepository = new RecordingSubscriptionRepository([activeSubscription]);
        var webPushClient = new StubWebPushClientAdapter();
        var sender = new WebPushNotificationSender(
            CreateAudienceService(subscriptionRepository, new SingleUserRepository(user)),
            new StubNotificationTextRenderer(),
            Microsoft.Extensions.Options.Options.Create(new WebPushOptions {
                Enabled = true,
                Subject = "https://example.com",
                PublicKey = "public",
                PrivateKey = "private",
                DefaultUrl = "/",
            }),
            webPushClient,
            FixedTime,
            NullLogger<WebPushNotificationSender>.Instance);
        var notification = Notification.Create(user.Id, NotificationTypes.FastingCompleted, "{}");

        await sender.SendAsync(notification, CancellationToken.None);

        Assert.Equal(1, webPushClient.SendCalls);
        Assert.Empty(subscriptionRepository.DeletedSubscriptions);
    }

    private static T InvokePrivate<T>(object instance, string methodName, params object[] args) {
        System.Reflection.MethodInfo method = instance.GetType().GetMethod(
            methodName,
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!;
        return (T)method.Invoke(instance, args)!;
    }

    private static readonly DateTime FixedNow = new(2026, 7, 1, 8, 0, 0, DateTimeKind.Utc);
    private static readonly TimeProvider FixedTime = new FixedTimeProvider();

    [ExcludeFromCodeCoverage]
    private sealed class FixedTimeProvider : TimeProvider {
        public override DateTimeOffset GetUtcNow() => new(FixedNow);
    }

    private static T InvokePrivateStatic<T>(string methodName, params object[] args) {
        System.Reflection.MethodInfo method = typeof(WebPushNotificationSender).GetMethod(
            methodName,
            System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic)!;
        return (T)method.Invoke(null, args)!;
    }

    private static T InvokeAudiencePrivateStatic<T>(string methodName, params object[] args) {
        System.Reflection.MethodInfo method = typeof(WebPushDeliveryAudienceService).GetMethod(
            methodName,
            System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic)!;
        return (T)method.Invoke(null, args)!;
    }

    private static WebPushDeliveryAudienceService CreateAudienceService(
        IWebPushSubscriptionRepository subscriptionRepository,
        IUserRepository userRepository) =>
        new(subscriptionRepository, subscriptionRepository, new UserNotificationProfileAdapter(userRepository));

    [ExcludeFromCodeCoverage]
    private sealed class UserNotificationProfileAdapter(IUserRepository repository) : IUserNotificationProfileService {
        public async Task<Result<UserNotificationProfileModel>> GetAsync(
            UserId userId,
            CancellationToken cancellationToken = default) {
            User? user = await repository.GetByIdAsync(userId, cancellationToken);
            if (user is null) {
                return Result.Failure<UserNotificationProfileModel>(Errors.Authentication.InvalidToken);
            }

            return Result.Success(ToNotificationProfile(user));
        }

        public Task<Result<UserNotificationProfileModel>> UpdatePreferencesAsync(
            UserId userId,
            UserPreferenceUpdate update,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }

    private static UserNotificationProfileModel ToNotificationProfile(User user) =>
        new(
            user.Id,
            user.HasPassword,
            user.Language,
            user.PushNotificationsEnabled,
            user.FastingPushNotificationsEnabled,
            user.SocialPushNotificationsEnabled,
            user.FastingCheckInReminderHours,
            user.FastingCheckInFollowUpReminderHours);

    [ExcludeFromCodeCoverage]
    private sealed class RecordingSubscriptionRepository(IEnumerable<WebPushSubscription>? subscriptions = null) : IWebPushSubscriptionRepository {
        public int GetByUserCalls { get; private set; }
        public List<WebPushSubscription> DeletedSubscriptions { get; } = [];

        public Task<WebPushSubscription?> GetByEndpointAsync(string endpoint, bool asTracking = false, CancellationToken cancellationToken = default) =>
            Task.FromResult<WebPushSubscription?>(null);

        public Task<IReadOnlyList<WebPushSubscription>> GetByUserAsync(UserId userId, CancellationToken cancellationToken = default) {
            GetByUserCalls++;
            return Task.FromResult<IReadOnlyList<WebPushSubscription>>(subscriptions?.ToList() ?? []);
        }

        public Task<IReadOnlyList<WebPushSubscriptionReadModel>> GetByUserReadModelsAsync(UserId userId, CancellationToken cancellationToken = default) {
            IReadOnlyList<WebPushSubscriptionReadModel> items = [.. (subscriptions?.ToList() ?? [])
                .Where(subscription => subscription.UserId == userId)
                .Select(subscription => new WebPushSubscriptionReadModel(
                    subscription.Endpoint,
                    subscription.ExpirationTimeUtc,
                    subscription.Locale,
                    subscription.UserAgent,
                    subscription.CreatedOnUtc,
                    subscription.ModifiedOnUtc))];
            return Task.FromResult(items);
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

    [ExcludeFromCodeCoverage]
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

    [ExcludeFromCodeCoverage]
    private sealed class RecordingUserRepository(User? user) : IUserRepository {
        public int GetByIdCalls { get; private set; }

        public Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<User?> GetByEmailIncludingDeletedAsync(string email, CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<User?> GetByIdAsync(UserId id, CancellationToken cancellationToken = default) {
            GetByIdCalls++;
            return Task.FromResult(user?.Id == id ? user : null);
        }

        public Task<User?> GetByIdIncludingDeletedAsync(UserId id, CancellationToken cancellationToken = default) => Task.FromResult<User?>(user?.Id == id ? user : null);
        public Task<User?> GetByTelegramUserIdAsync(long telegramUserId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<User?> GetByTelegramUserIdIncludingDeletedAsync(long telegramUserId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<(IReadOnlyList<User> Items, int TotalItems)> GetPagedAsync(string? search, int page, int limit, bool includeDeleted, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<(int TotalUsers, int ActiveUsers, int PremiumUsers, int DeletedUsers, IReadOnlyList<User> RecentUsers)> GetAdminDashboardSummaryAsync(int recentLimit, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<IReadOnlyList<Role>> GetRolesByNamesAsync(IReadOnlyList<string> names, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<User> AddAsync(User userToAdd, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task UpdateAsync(User userToUpdate, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    [ExcludeFromCodeCoverage]
    private sealed class NullUserRepository : IUserRepository {
        public Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<User?> GetByEmailIncludingDeletedAsync(string email, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<User?> GetByIdAsync(UserId id, CancellationToken cancellationToken = default) => Task.FromResult<User?>(null);
        public Task<User?> GetByIdIncludingDeletedAsync(UserId id, CancellationToken cancellationToken = default) => Task.FromResult<User?>(null);
        public Task<User?> GetByTelegramUserIdAsync(long telegramUserId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<User?> GetByTelegramUserIdIncludingDeletedAsync(long telegramUserId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<(IReadOnlyList<User> Items, int TotalItems)> GetPagedAsync(string? search, int page, int limit, bool includeDeleted, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<(int TotalUsers, int ActiveUsers, int PremiumUsers, int DeletedUsers, IReadOnlyList<User> RecentUsers)> GetAdminDashboardSummaryAsync(int recentLimit, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<IReadOnlyList<Role>> GetRolesByNamesAsync(IReadOnlyList<string> names, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<User> AddAsync(User userToAdd, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task UpdateAsync(User userToUpdate, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    [ExcludeFromCodeCoverage]
    private sealed class StubNotificationTextRenderer : INotificationTextRenderer {
        public NotificationText Render(string type, string? locale = null, params object[] arguments) =>
            new("Title", "Body");

        public NotificationText RenderFromPayload(string type, string payloadJson, string? locale = null) =>
            new("Title", "Body");
    }

    [ExcludeFromCodeCoverage]
    private sealed class StubWebPushClientAdapter(Exception? exception = null) : IWebPushClientAdapter {
        private int _sendCalls;

        public int SendCalls => Volatile.Read(ref _sendCalls);

        public Task SendNotificationAsync(
            PushSubscription subscription,
            string payload,
            VapidDetails vapidDetails,
            CancellationToken cancellationToken) {
            Interlocked.Increment(ref _sendCalls);
            return exception is null
                ? Task.CompletedTask
                : Task.FromException(exception);
        }
    }
}
