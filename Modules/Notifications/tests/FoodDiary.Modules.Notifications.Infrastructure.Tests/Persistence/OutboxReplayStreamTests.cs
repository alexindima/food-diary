using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Modules.Notifications.Infrastructure.Persistence;
using FoodDiary.Application.Abstractions.Common.Abstractions.Outbox;
using FoodDiary.Infrastructure.Persistence;
using FoodDiary.Infrastructure.Persistence.Notifications;
using FoodDiary.Infrastructure.Persistence.Outbox;
using FoodDiary.Modules.Notifications.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Infrastructure.Tests.Persistence;

[ExcludeFromCodeCoverage]
public sealed class OutboxReplayStreamTests {
    [Fact]
    public async Task Adapter_PreservesProjectionTrackingFiltersAndBoundedList() {
        DateTime now = new(2026, 9, 3, 10, 0, 0, DateTimeKind.Utc);
        await using FoodDiaryDbContext context = new(new DbContextOptionsBuilder<FoodDiaryDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString(), new Microsoft.EntityFrameworkCore.Storage.InMemoryDatabaseRoot()).Options);
        var services = new ServiceCollection();
        services.AddSingleton(context);
        services.AddNotificationsPersistence();
        services.AddNotificationsPersistence();
        await using ServiceProvider provider = services.BuildServiceProvider();
        IOutboxReplayStream stream = Assert.Single(provider.GetServices<IOutboxReplayStream>());
        NotificationsDbContext owned = provider.GetRequiredService<NotificationsDbContext>();
        Assert.Equal(ServiceLifetime.Scoped, Assert.Single(services, descriptor => descriptor.ServiceType == typeof(IOutboxReplayStream)).Lifetime);
        Assert.Equal("notification_web_push", stream.Name);
        Assert.Equal(2, stream.Order);
        Assert.Null(stream.ReplayRejectionReason);
        var message = NotificationWebPushOutboxMessage.Create(NotificationId.New(), now.AddMinutes(-3));
        message.MarkDeadLettered("failure", now);
        context.Add(message);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
        owned.ChangeTracker.Clear();

        OutboxDeadLetterMessageModel listed = Assert.Single(await stream.ListAsync(1));
        Assert.Empty(owned.ChangeTracker.Entries());
        Assert.Multiple(
            () => Assert.Equal(message.Id, listed.MessageId),
            () => Assert.Equal(message.NotificationId.Value.ToString(), listed.Summary),
            () => Assert.Equal("failure", listed.LastError),
            () => Assert.Equal(1, listed.AttemptCount));
        Assert.Null(await stream.FindAsync(Guid.NewGuid(), forUpdate: false));
        Assert.Empty(owned.ChangeTracker.Entries());
        OutboxReplayEntry? found = await stream.FindAsync(message.Id, forUpdate: false);
        Assert.NotNull(found);
        Assert.Same(owned.ChangeTracker.Entries<NotificationWebPushOutboxMessage>().Single().Entity, found.Message);
        Assert.Equal(EntityState.Unchanged, owned.Entry(found.Message).State);
        Assert.Equal(listed.Summary, found.Summary);
        Assert.Equal(listed.LastError, found.LastError);

        var active = NotificationWebPushOutboxMessage.Create(NotificationId.New(), now.AddMinutes(-3));
        var processed = NotificationWebPushOutboxMessage.Create(NotificationId.New(), now.AddMinutes(-3));
        processed.MarkDeadLettered("processed", now.AddMinutes(1));
        processed.MarkProcessed(now.AddMinutes(2));
        context.AddRange(active, processed);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
        owned.ChangeTracker.Clear();
        Assert.Equal(message.Id, Assert.Single(await stream.ListAsync(1)).MessageId);
        Assert.Empty(owned.ChangeTracker.Entries());
        OutboxReplayEntry? activeEntry = await stream.FindAsync(active.Id, forUpdate: false);
        Assert.NotNull(activeEntry);
        Assert.Equal(active.Id, activeEntry.Message.Id);
        Assert.Equal(EntityState.Unchanged, owned.Entry(activeEntry.Message).State);
        context.ChangeTracker.Clear();
        owned.ChangeTracker.Clear();
        Assert.Null(await stream.FindAsync(Guid.NewGuid(), forUpdate: false));
        Assert.Empty(owned.ChangeTracker.Entries());
    }
}
