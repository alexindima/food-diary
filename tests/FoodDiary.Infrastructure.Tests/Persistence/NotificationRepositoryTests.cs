using System.Reflection;
using FoodDiary.Application.Abstractions.Notifications.Common;
using FoodDiary.Domain.Entities.Notifications;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Infrastructure.Persistence;
using FoodDiary.Infrastructure.Persistence.Notifications;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Infrastructure.Tests.Persistence;

public sealed class NotificationRepositoryTests {
    [Fact]
    public async Task DeleteExpiredBatchAsync_WhenReadRecently_KeepsOldCreatedNotification() {
        await using var context = CreateContext();
        var user = User.Create("recent-read-notification@example.com", "hash");
        var notification = Notification.Create(user.Id, NotificationTypes.FastingCompleted, NotificationPayloads.Empty());
        SetAuditState(
            notification,
            createdOnUtc: new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            isRead: true,
            readAtUtc: new DateTime(2026, 5, 20, 0, 0, 0, DateTimeKind.Utc));

        context.Users.Add(user);
        context.Notifications.Add(notification);
        await context.SaveChangesAsync();

        var repository = new NotificationRepository(context);

        var deleted = await repository.DeleteExpiredBatchAsync(
            [],
            transientReadOlderThanUtc: RetentionCutoff,
            transientUnreadOlderThanUtc: RetentionCutoff,
            standardReadOlderThanUtc: RetentionCutoff,
            standardUnreadOlderThanUtc: RetentionCutoff,
            batchSize: 10);

        Assert.Equal(0, deleted);
        Assert.True(await context.Notifications.AnyAsync(n => n.Id == notification.Id));
    }

    [Fact]
    public async Task DeleteExpiredBatchAsync_WhenReadBeforeRetentionCutoff_DeletesByReadTimestamp() {
        await using var context = CreateContext();
        var user = User.Create("expired-read-notification@example.com", "hash");
        var notification = Notification.Create(user.Id, NotificationTypes.FastingCompleted, NotificationPayloads.Empty());
        SetAuditState(
            notification,
            createdOnUtc: new DateTime(2026, 5, 20, 0, 0, 0, DateTimeKind.Utc),
            isRead: true,
            readAtUtc: new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc));

        context.Users.Add(user);
        context.Notifications.Add(notification);
        await context.SaveChangesAsync();

        var repository = new NotificationRepository(context);

        var deleted = await repository.DeleteExpiredBatchAsync(
            [],
            transientReadOlderThanUtc: RetentionCutoff,
            transientUnreadOlderThanUtc: RetentionCutoff,
            standardReadOlderThanUtc: RetentionCutoff,
            standardUnreadOlderThanUtc: RetentionCutoff,
            batchSize: 10);

        Assert.Equal(1, deleted);
        Assert.False(await context.Notifications.AnyAsync(n => n.Id == notification.Id));
    }

    private static DateTime RetentionCutoff => new(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc);

    private static FoodDiaryDbContext CreateContext() {
        var options = new DbContextOptionsBuilder<FoodDiaryDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;

        return new FoodDiaryDbContext(options);
    }

    private static void SetAuditState(
        Notification notification,
        DateTime createdOnUtc,
        bool isRead,
        DateTime? readAtUtc) {
        SetProperty(notification, nameof(Notification.CreatedOnUtc), createdOnUtc);
        SetProperty(notification, nameof(Notification.IsRead), isRead);
        SetProperty(notification, nameof(Notification.ReadAtUtc), readAtUtc);
    }

    private static void SetProperty(Notification notification, string propertyName, object? value) {
        var property = FindProperty(typeof(Notification), propertyName);
        var setter = property.GetSetMethod(nonPublic: true)
            ?? throw new InvalidOperationException($"Property '{propertyName}' does not have a setter.");

        setter.Invoke(notification, [value]);
    }

    private static PropertyInfo FindProperty(Type type, string propertyName) {
        for (var current = type; current is not null; current = current.BaseType) {
            var property = current.GetProperty(
                propertyName,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
            if (property is not null) {
                return property;
            }
        }

        throw new InvalidOperationException($"Property '{propertyName}' was not found.");
    }
}
