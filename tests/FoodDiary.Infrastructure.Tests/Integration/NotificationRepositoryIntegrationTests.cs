using FoodDiary.Application.Abstractions.Notifications.Common;
using FoodDiary.Domain.Entities.Notifications;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Infrastructure.Persistence;
using FoodDiary.Infrastructure.Persistence.Notifications;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace FoodDiary.Infrastructure.Tests.Integration;

[Collection(PostgresDatabaseCollection.Name)]
[ExcludeFromCodeCoverage]
public sealed class NotificationRepositoryIntegrationTests(PostgresDatabaseFixture databaseFixture) {
    [RequiresDockerFact]
    public async Task DeleteExpiredBatchAsync_WhenReadRecently_KeepsOldCreatedNotification() {
        await using FoodDiaryDbContext context = await databaseFixture.CreateDbContextAsync();
        var user = User.Create("recent-read-notification@example.com", "hash");
        var notification = Notification.Create(user.Id, NotificationTypes.FastingCompleted, NotificationPayloads.Empty());

        context.Users.Add(user);
        context.Notifications.Add(notification);
        await context.SaveChangesAsync();

        var createdBeforeRetentionCutoff = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var readAfterRetentionCutoff = new DateTime(2026, 5, 20, 0, 0, 0, DateTimeKind.Utc);

        await context.Notifications
            .Where(n => n.Id == notification.Id)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(n => n.CreatedOnUtc, createdBeforeRetentionCutoff)
                .SetProperty(n => n.IsRead, valueExpression: true)
                .SetProperty(n => n.ReadAtUtc, readAfterRetentionCutoff));

        var repository = new NotificationRepository(context);
        int deleted = await repository.DeleteExpiredBatchAsync(
            [],
            transientReadOlderThanUtc: new DateTime(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc),
            transientUnreadOlderThanUtc: new DateTime(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc),
            standardReadOlderThanUtc: new DateTime(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc),
            standardUnreadOlderThanUtc: new DateTime(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc),
            batchSize: 10);

        await using FoodDiaryDbContext verificationContext = CreateVerificationContext(context);

        Assert.Equal(0, deleted);
        Assert.True(await verificationContext.Notifications.AnyAsync(n => n.Id == notification.Id));
    }

    [RequiresDockerFact]
    public async Task DeleteExpiredBatchAsync_WhenReadBeforeRetentionCutoff_DeletesByReadTimestamp() {
        await using FoodDiaryDbContext context = await databaseFixture.CreateDbContextAsync();
        var user = User.Create("expired-read-notification@example.com", "hash");
        var notification = Notification.Create(user.Id, NotificationTypes.FastingCompleted, NotificationPayloads.Empty());

        context.Users.Add(user);
        context.Notifications.Add(notification);
        await context.SaveChangesAsync();

        await context.Notifications
            .Where(n => n.Id == notification.Id)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(n => n.CreatedOnUtc, new DateTime(2026, 5, 20, 0, 0, 0, DateTimeKind.Utc))
                .SetProperty(n => n.IsRead, valueExpression: true)
                .SetProperty(n => n.ReadAtUtc, new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc)));

        var repository = new NotificationRepository(context);
        int deleted = await repository.DeleteExpiredBatchAsync(
            [],
            transientReadOlderThanUtc: new DateTime(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc),
            transientUnreadOlderThanUtc: new DateTime(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc),
            standardReadOlderThanUtc: new DateTime(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc),
            standardUnreadOlderThanUtc: new DateTime(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc),
            batchSize: 10);

        await using FoodDiaryDbContext verificationContext = CreateVerificationContext(context);

        Assert.Equal(1, deleted);
        Assert.False(await verificationContext.Notifications.AnyAsync(n => n.Id == notification.Id));
    }

    private static FoodDiaryDbContext CreateVerificationContext(FoodDiaryDbContext sourceContext) {
        string connectionString = sourceContext.Database.GetConnectionString()
            ?? throw new InvalidOperationException("Source context does not have a connection string.");

        DbContextOptions<FoodDiaryDbContext> options = new DbContextOptionsBuilder<FoodDiaryDbContext>()
            .UseNpgsql(new NpgsqlConnectionStringBuilder(connectionString).ConnectionString)
            .Options;

        return new FoodDiaryDbContext(options);
    }
}
