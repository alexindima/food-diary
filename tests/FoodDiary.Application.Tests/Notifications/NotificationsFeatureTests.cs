using FoodDiary.Application.Notifications.Commands.MarkAllNotificationsRead;
using FoodDiary.Application.Notifications.Commands.MarkNotificationRead;
using FoodDiary.Application.Notifications.Commands.UpdateNotificationPreferences;
using FoodDiary.Application.Notifications.Common;
using FoodDiary.Application.Notifications.Queries.GetUnreadCount;
using FoodDiary.Application.Common.Abstractions.Audit;
using FoodDiary.Application.Common.Interfaces.Persistence;
using FoodDiary.Domain.Entities.Notifications;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Tests.Notifications;

public class NotificationsFeatureTests {
    [Fact]
    public async Task MarkNotificationRead_WithValidOwnership_Succeeds() {
        var userId = UserId.New();
        var notification = Notification.Create(userId, "info", "{}");
        var repo = new InMemoryNotificationRepository();
        repo.Seed(notification);

        var handler = new MarkNotificationReadCommandHandler(repo);
        var result = await handler.Handle(
            new MarkNotificationReadCommand(userId.Value, notification.Id.Value),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(notification.IsRead);
    }

    [Fact]
    public async Task MarkNotificationRead_WhenNotOwned_ReturnsFailure() {
        var ownerId = UserId.New();
        var otherUserId = UserId.New();
        var notification = Notification.Create(ownerId, "info", "{}");
        var repo = new InMemoryNotificationRepository();
        repo.Seed(notification);

        var handler = new MarkNotificationReadCommandHandler(repo);
        var result = await handler.Handle(
            new MarkNotificationReadCommand(otherUserId.Value, notification.Id.Value),
            CancellationToken.None);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task MarkNotificationRead_WhenNotFound_ReturnsFailure() {
        var repo = new InMemoryNotificationRepository();
        var handler = new MarkNotificationReadCommandHandler(repo);

        var result = await handler.Handle(
            new MarkNotificationReadCommand(Guid.NewGuid(), Guid.NewGuid()),
            CancellationToken.None);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task MarkNotificationRead_WithNullUserId_ReturnsFailure() {
        var handler = new MarkNotificationReadCommandHandler(new InMemoryNotificationRepository());

        var result = await handler.Handle(
            new MarkNotificationReadCommand(null, Guid.NewGuid()),
            CancellationToken.None);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task MarkAllNotificationsRead_WithValidUserId_Succeeds() {
        var repo = new InMemoryNotificationRepository();
        var handler = new MarkAllNotificationsReadCommandHandler(repo);

        var result = await handler.Handle(
            new MarkAllNotificationsReadCommand(Guid.NewGuid()),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(repo.MarkAllReadCalled);
    }

    [Fact]
    public async Task MarkAllNotificationsRead_WithNullUserId_ReturnsFailure() {
        var handler = new MarkAllNotificationsReadCommandHandler(new InMemoryNotificationRepository());

        var result = await handler.Handle(
            new MarkAllNotificationsReadCommand(null),
            CancellationToken.None);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task GetUnreadCount_ReturnsCount() {
        var userId = UserId.New();
        var repo = new InMemoryNotificationRepository();
        repo.Seed(Notification.Create(userId, "info", "{}"));
        repo.Seed(Notification.Create(userId, "info", "{}"));

        var handler = new GetUnreadCountQueryHandler(repo);
        var result = await handler.Handle(
            new GetUnreadCountQuery(userId.Value),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value);
    }

    [Fact]
    public async Task UpdateNotificationPreferences_UpdatesUserAndWritesAuditLog() {
        var user = User.Create("notifications@example.com", "hash");
        var userRepository = new SingleUserRepository(user);
        var auditLogger = new RecordingAuditLogger();
        var handler = new UpdateNotificationPreferencesCommandHandler(userRepository, auditLogger);

        var result = await handler.Handle(
            new UpdateNotificationPreferencesCommand(user.Id.Value, true, false, true, 12, 20),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(user.PushNotificationsEnabled);
        Assert.False(user.FastingPushNotificationsEnabled);
        Assert.True(user.SocialPushNotificationsEnabled);
        Assert.Equal(12, user.FastingCheckInReminderHours);
        Assert.Equal(20, user.FastingCheckInFollowUpReminderHours);
        Assert.Equal("notifications.preferences.updated", auditLogger.Action);
        Assert.Equal(user.Id, auditLogger.ActorId);
        Assert.Contains("push=True", auditLogger.Details);
        Assert.Contains("fasting=False", auditLogger.Details);
        Assert.Contains("social=True", auditLogger.Details);
        Assert.Contains("fastingReminder=12", auditLogger.Details);
        Assert.Contains("fastingReminderFollowUp=20", auditLogger.Details);
    }

    private sealed class InMemoryNotificationRepository : INotificationRepository {
        private readonly List<Notification> _notifications = [];
        public bool MarkAllReadCalled { get; private set; }

        public void Seed(Notification notification) => _notifications.Add(notification);

        public Task<Notification?> GetByIdAsync(NotificationId id, bool asTracking = false, CancellationToken ct = default) =>
            Task.FromResult(_notifications.FirstOrDefault(n => n.Id == id));

        public Task<Notification> AddAsync(Notification notification, CancellationToken ct = default) {
            _notifications.Add(notification);
            return Task.FromResult(notification);
        }

        public Task UpdateAsync(Notification notification, CancellationToken ct = default) => Task.CompletedTask;

        public Task<bool> ExistsAsync(UserId userId, string type, string referenceId, CancellationToken ct = default) =>
            Task.FromResult(_notifications.Any(n => n.UserId == userId && n.Type == type && n.ReferenceId == referenceId));

        public Task<int> GetUnreadCountAsync(UserId userId, CancellationToken ct = default) =>
            Task.FromResult(_notifications.Count(n => n.UserId == userId && !n.IsRead));

        public Task MarkAllReadAsync(UserId userId, CancellationToken ct = default) {
            MarkAllReadCalled = true;
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<Notification>> GetByUserAsync(UserId userId, int limit = 50, CancellationToken ct = default) {
            var items = _notifications.Where(n => n.UserId == userId).Take(limit).ToList();
            return Task.FromResult<IReadOnlyList<Notification>>(items);
        }

        public Task<int> DeleteExpiredBatchAsync(
            IReadOnlyCollection<string> transientTypes,
            DateTime transientReadOlderThanUtc,
            DateTime transientUnreadOlderThanUtc,
            DateTime standardReadOlderThanUtc,
            DateTime standardUnreadOlderThanUtc,
            int batchSize,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(0);
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

    private sealed class RecordingAuditLogger : IAuditLogger {
        public string Action { get; private set; } = string.Empty;
        public UserId ActorId { get; private set; } = UserId.Empty;
        public string? Details { get; private set; }

        public void Log(string action, UserId actorId, string? targetType = null, string? targetId = null, string? details = null) {
            Action = action;
            ActorId = actorId;
            Details = details;
        }
    }
}
