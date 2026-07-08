using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Abstractions.Notifications.Common;
using FoodDiary.Application.Abstractions.Notifications.Models;
using FoodDiary.Application.Notifications.Common;
using FoodDiary.Application.Notifications.Services;
using FoodDiary.Application.Abstractions.Common.Abstractions.Audit;
using FoodDiary.Application.Abstractions.Common.Abstractions.Persistence;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Domain.Entities.Notifications;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Results;

namespace FoodDiary.Application.Tests.Notifications;

[ExcludeFromCodeCoverage]
public partial class NotificationsFeatureTests {
    private static NotificationReadModel ToReadModel(Notification notification) =>
        new(notification.Id.Value, notification.Type, notification.ReferenceId, notification.PayloadJson, notification.IsRead, notification.CreatedOnUtc);

    private static WebPushSubscriptionReadModel ToReadModel(WebPushSubscription subscription) =>
        new(subscription.Endpoint, subscription.ExpirationTimeUtc, subscription.Locale, subscription.UserAgent, subscription.CreatedOnUtc, subscription.ModifiedOnUtc);
    private static User CreateUser(UserId? id = null, string email = "notifications@example.com") {
        var user = User.Create(email, "hash");
        if (id is not null) {
            typeof(User).GetProperty(nameof(User.Id))!.SetValue(user, id);
        }

        return user;
    }

    private static User CreateDeletedUser(UserId id) {
        User user = CreateUser(id, "deleted-notifications@example.com");
        user.DeleteAccount(DateTime.UtcNow);
        return user;
    }

    private static IUnitOfWork CreateUnitOfWork() {
        IUnitOfWork unitOfWork = Substitute.For<IUnitOfWork>();
        unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
        return unitOfWork;
    }

    private static RecordingPostCommitActionQueue CreatePostCommitActionQueue() => new();




















































    [ExcludeFromCodeCoverage]
    private sealed class InMemoryNotificationRepository : INotificationRepository {
        private readonly List<Notification> _notifications = [];
        public IReadOnlyList<Notification> Notifications => _notifications;
        public bool MarkAllReadCalled { get; private set; }
        public bool DeleteExpiredBatchCalled { get; private set; }
        public IReadOnlyCollection<string> TransientTypes { get; private set; } = [];
        public DateTime TransientReadOlderThanUtc { get; private set; }
        public DateTime TransientUnreadOlderThanUtc { get; private set; }
        public DateTime StandardReadOlderThanUtc { get; private set; }
        public DateTime StandardUnreadOlderThanUtc { get; private set; }
        public int BatchSize { get; private set; }
        public int DeleteExpiredBatchResult { get; init; }
        public CancellationToken DeleteExpiredBatchCancellationToken { get; private set; }

        public void Seed(Notification notification) => _notifications.Add(notification);

        public Task<Notification?> GetByIdAsync(NotificationId id, bool asTracking = false, CancellationToken ct = default) =>
            Task.FromResult(_notifications.FirstOrDefault(n => n.Id == id));

        public Task<Notification> AddAsync(Notification notification, CancellationToken ct = default) {
            _notifications.Add(notification);
            return Task.FromResult(notification);
        }

        public Task UpdateAsync(Notification notification, CancellationToken ct = default) => Task.CompletedTask;

        public Task<bool> ExistsAsync(UserId userId, string type, string referenceId, CancellationToken ct = default) =>
            Task.FromResult(_notifications.Any(n => n.UserId == userId && string.Equals(n.Type, type, StringComparison.Ordinal) && string.Equals(n.ReferenceId, referenceId, StringComparison.Ordinal)));

        public Task<int> GetUnreadCountAsync(UserId userId, CancellationToken ct = default) =>
            Task.FromResult(_notifications.Count(n => n.UserId == userId && !n.IsRead));

        public Task<int> GetUnreadCountAsync(UserId userId, string type, CancellationToken ct = default) =>
            Task.FromResult(_notifications.Count(n => n.UserId == userId && !n.IsRead && string.Equals(n.Type, type, StringComparison.Ordinal)));

        public Task MarkAllReadAsync(UserId userId, CancellationToken ct = default) {
            MarkAllReadCalled = true;
            foreach (Notification? notification in _notifications.Where(n => n.UserId == userId)) {
                notification.MarkAsRead();
            }

            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<Notification>> GetByUserAsync(UserId userId, int limit = 50, CancellationToken ct = default) {
            var items = _notifications.Where(n => n.UserId == userId).Take(limit).ToList();
            return Task.FromResult<IReadOnlyList<Notification>>(items);
        }

        public Task<IReadOnlyList<NotificationReadModel>> GetByUserReadModelsAsync(UserId userId, int limit = 50, CancellationToken ct = default) {
            var items = _notifications.Where(n => n.UserId == userId).Take(limit).Select(ToReadModel).ToList();
            return Task.FromResult<IReadOnlyList<NotificationReadModel>>(items);
        }

        public Task<int> DeleteExpiredBatchAsync(
            IReadOnlyCollection<string> transientTypes,
            DateTime transientReadOlderThanUtc,
            DateTime transientUnreadOlderThanUtc,
            DateTime standardReadOlderThanUtc,
            DateTime standardUnreadOlderThanUtc,
            int batchSize,
            CancellationToken cancellationToken = default) {
            DeleteExpiredBatchCalled = true;
            TransientTypes = transientTypes;
            TransientReadOlderThanUtc = transientReadOlderThanUtc;
            TransientUnreadOlderThanUtc = transientUnreadOlderThanUtc;
            StandardReadOlderThanUtc = standardReadOlderThanUtc;
            StandardUnreadOlderThanUtc = standardUnreadOlderThanUtc;
            BatchSize = batchSize;
            DeleteExpiredBatchCancellationToken = cancellationToken;
            return Task.FromResult(DeleteExpiredBatchResult);
        }
    }

    [ExcludeFromCodeCoverage]
    private sealed class RecordingNotificationWebPushOutbox : INotificationWebPushOutbox {
        public List<NotificationId> NotificationIds { get; } = [];

        public Task EnqueueAsync(NotificationId notificationId, CancellationToken cancellationToken = default) {
            NotificationIds.Add(notificationId);
            return Task.CompletedTask;
        }
    }

    [ExcludeFromCodeCoverage]
    private sealed class RecordingNotificationPusher : INotificationPusher {
        public Guid? UnreadCountUserId { get; private set; }
        public int? UnreadCount { get; private set; }
        public Guid? NotificationsChangedUserId { get; private set; }

        public Task PushUnreadCountAsync(Guid userId, int count, CancellationToken cancellationToken = default) {
            UnreadCountUserId = userId;
            UnreadCount = count;
            return Task.CompletedTask;
        }

        public Task PushNotificationsChangedAsync(Guid userId, CancellationToken cancellationToken = default) {
            NotificationsChangedUserId = userId;
            return Task.CompletedTask;
        }
    }

    [ExcludeFromCodeCoverage]
    private sealed class RecordingPostCommitActionQueue : IPostCommitActionQueue {
        private readonly List<Func<CancellationToken, Task>> actions = [];

        public bool HasActions => actions.Count > 0;

        public void Enqueue(string actionName, Func<CancellationToken, Task> action) => actions.Add(action);

        public async Task FlushAsync(CancellationToken cancellationToken = default) {
            Func<CancellationToken, Task>[] pendingActions = [.. actions];
            actions.Clear();

            foreach (Func<CancellationToken, Task> action in pendingActions) {
                await action(cancellationToken).ConfigureAwait(false);
            }
        }
    }

    [ExcludeFromCodeCoverage]
    private sealed class InMemoryWebPushSubscriptionRepository(
        IReadOnlyList<WebPushSubscription>? subscriptions = null)
        : IWebPushSubscriptionRepository {
        private readonly List<WebPushSubscription> _subscriptions = subscriptions?.ToList() ?? [];

        public IReadOnlyList<WebPushSubscription> Subscriptions => _subscriptions;
        public List<string> DeletedEndpoints { get; } = [];
        public int EndpointLookupCount { get; private set; }
        public int UpdateCallCount { get; private set; }

        public Task<WebPushSubscription?> GetByEndpointAsync(
            string endpoint,
            bool asTracking = false,
            CancellationToken cancellationToken = default) {
            EndpointLookupCount++;
            return Task.FromResult<WebPushSubscription?>(_subscriptions.FirstOrDefault(subscription => string.Equals(subscription.Endpoint, endpoint, StringComparison.Ordinal)));
        }

        public Task<IReadOnlyList<WebPushSubscription>> GetByUserAsync(
            UserId userId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<WebPushSubscription>>(
                _subscriptions.Where(subscription => subscription.UserId == userId).ToList());

        public Task<IReadOnlyList<WebPushSubscriptionReadModel>> GetByUserReadModelsAsync(
            UserId userId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<WebPushSubscriptionReadModel>>(
                [.. _subscriptions.Where(subscription => subscription.UserId == userId).Select(ToReadModel)]);

        public Task<WebPushSubscription> AddAsync(
            WebPushSubscription subscription,
            CancellationToken cancellationToken = default) {
            _subscriptions.Add(subscription);
            return Task.FromResult(subscription);
        }

        public Task UpdateAsync(WebPushSubscription subscription, CancellationToken cancellationToken = default) {
            UpdateCallCount++;
            return Task.CompletedTask;
        }

        public Task DeleteAsync(WebPushSubscription subscription, CancellationToken cancellationToken = default) {
            DeletedEndpoints.Add(subscription.Endpoint);
            _subscriptions.Remove(subscription);
            return Task.CompletedTask;
        }

        public Task DeleteRangeAsync(
            IReadOnlyCollection<WebPushSubscription> subscriptionsToDelete,
            CancellationToken cancellationToken = default) {
            foreach (WebPushSubscription subscription in subscriptionsToDelete) {
                DeletedEndpoints.Add(subscription.Endpoint);
                _subscriptions.Remove(subscription);
            }

            return Task.CompletedTask;
        }
    }

    [ExcludeFromCodeCoverage]
    private sealed class RecordingNotificationTestScheduler(ScheduledNotificationData scheduled) : INotificationTestScheduler {
        public bool WasCalled { get; private set; }
        public Guid UserId { get; private set; }
        public int DelaySeconds { get; private set; }
        public string Type { get; private set; } = string.Empty;

        public Task<ScheduledNotificationData> ScheduleAsync(
            Guid userId,
            int delaySeconds,
            string type,
            CancellationToken cancellationToken) {
            WasCalled = true;
            UserId = userId;
            DelaySeconds = delaySeconds;
            Type = type;
            return Task.FromResult(scheduled);
        }
    }

    [ExcludeFromCodeCoverage]
    private sealed class StaticWebPushConfigurationProvider(WebPushClientConfiguration configuration) : IWebPushConfigurationProvider {
        public WebPushClientConfiguration GetClientConfiguration() => configuration;
    }

    private static ICurrentUserAccessService CreateCurrentUserAccessService(User user) {
        ICurrentUserAccessService service = Substitute.For<ICurrentUserAccessService>();
        Error? error = user.DeletedAt is null ? null : Errors.Authentication.AccountDeleted;
        service
            .EnsureCanAccessAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns(call => {
                UserId id = call.Arg<UserId>();
                return Task.FromResult(user.Id == id ? error : Errors.Authentication.InvalidToken);
            });
        return service;
    }

    private static INotificationPreferencesService CreateNotificationPreferencesService(User user) =>
        new NotificationPreferencesService(new SingleUserRepository(user));

    private static INotificationUserContextService CreateNotificationUserContextService(User user) =>
        new NotificationUserContextService(new SingleUserRepository(user));

    private static INotificationUserAccessService CreateNotificationUserAccessService(User user) =>
        new SingleUserRepository(user);

    private static INotificationFeedReadService CreateNotificationFeedReadService(
        INotificationRepository notificationRepository,
        INotificationTextRenderer notificationTextRenderer) =>
        new NotificationFeedReadService(notificationRepository, notificationTextRenderer);

    private static IWebPushSubscriptionReadService CreateWebPushSubscriptionReadService(
        IWebPushSubscriptionReadModelRepository webPushSubscriptionRepository) =>
        new WebPushSubscriptionReadService(webPushSubscriptionRepository);

    [ExcludeFromCodeCoverage]
    private sealed class SingleUserRepository(User user) : IUserRepository, INotificationUserAccessService {
        public Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<User?> GetByEmailIncludingDeletedAsync(string email, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<User?> GetByIdAsync(UserId id, CancellationToken cancellationToken = default) => Task.FromResult<User?>(user.Id == id ? user : null);

        public Task<Result<User>> GetAccessibleUserAsync(UserId userId, CancellationToken cancellationToken) {
            User? foundUser = user.Id == userId ? user : null;
            if (foundUser is null) {
                return Task.FromResult(Result.Failure<User>(Errors.Authentication.InvalidToken));
            }

            if (foundUser.DeletedAt is not null) {
                return Task.FromResult(Result.Failure<User>(Errors.Authentication.AccountDeleted));
            }

            return Task.FromResult(Result.Success(foundUser));
        }

        public async Task<Error?> EnsureCanAccessAsync(UserId userId, CancellationToken cancellationToken = default) {
            Result<User> result = await GetAccessibleUserAsync(userId, cancellationToken).ConfigureAwait(false);
            return result.IsFailure ? result.Error : null;
        }

        public Task<User?> GetByIdIncludingDeletedAsync(UserId id, CancellationToken cancellationToken = default) => Task.FromResult<User?>(user.Id == id ? user : null);
        public Task<User?> GetByTelegramUserIdAsync(long telegramUserId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<User?> GetByTelegramUserIdIncludingDeletedAsync(long telegramUserId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<(IReadOnlyList<User> Items, int TotalItems)> GetPagedAsync(string? search, int page, int limit, bool includeDeleted, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<(int TotalUsers, int ActiveUsers, int PremiumUsers, int DeletedUsers, IReadOnlyList<User> RecentUsers)> GetAdminDashboardSummaryAsync(int recentLimit, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<IReadOnlyList<Role>> GetRolesByNamesAsync(IReadOnlyList<string> names, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<User> AddAsync(User userToAdd, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task UpdateAsync(User userToUpdate, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task UpdateUserAsync(User userToUpdate, CancellationToken cancellationToken) =>
            UpdateAsync(userToUpdate, cancellationToken);
    }

    [ExcludeFromCodeCoverage]
    private sealed class RecordingAuditLogger : IAuditLogger {
        public string Action { get; private set; } = string.Empty;
        public UserId ActorId { get; private set; } = UserId.Empty;
        public string? TargetId { get; private set; }
        public string? Details { get; private set; }

        public void Log(string action, UserId actorId, string? targetType = null, string? targetId = null, string? details = null) {
            Action = action;
            ActorId = actorId;
            TargetId = targetId;
            Details = details;
        }
    }

    [ExcludeFromCodeCoverage]
    private sealed class RecordingNotificationTextRenderer : INotificationTextRenderer {
        public List<string> RenderedTypes { get; } = [];

        public NotificationText Render(string type, string? locale = null, params object[] arguments) =>
            new(type, Body: null);

        public NotificationText RenderFromPayload(string type, string payloadJson, string? locale = null) {
            RenderedTypes.Add(type);
            return new NotificationText(type, payloadJson);
        }
    }

    [ExcludeFromCodeCoverage]
    private sealed class FixedDateTimeProvider(DateTime utcNow) : TimeProvider {
        public override DateTimeOffset GetUtcNow() => new(utcNow);
    }
}
