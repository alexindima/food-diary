using FoodDiary.Application.Notifications.Commands.MarkAllNotificationsRead;
using FoodDiary.Application.Notifications.Commands.MarkNotificationRead;
using FoodDiary.Application.Notifications.Common;
using FoodDiary.Application.Notifications.Queries.GetUnreadCount;
using FoodDiary.Domain.Entities.Notifications;
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
}
