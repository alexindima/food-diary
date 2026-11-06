using FoodDiary.Infrastructure.Persistence;
using FoodDiary.Modules.Notifications.Application.Abstractions.Common;
using FoodDiary.Modules.Notifications.Application.Abstractions.Models;
using FoodDiary.Modules.Notifications.Domain.Entities;
using FoodDiary.Modules.Notifications.Domain.ValueObjects.Ids;
using FoodDiary.Modules.Notifications.PersistenceModel;
using FoodDiary.Modules.Users.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Infrastructure.IntegrationTests.Integration;

public sealed partial class SharedDeliveryContextsIntegrationTests {
    [RequiresDockerTheory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task WebPushFairnessSurvivesScopesAndFailedDeliveryAsync(bool failFirst) {
        await using FoodDiaryDbContext central = await databaseFixture.CreateDbContextAsync();
        var noisy = User.Create($"noisy-{Guid.NewGuid():N}@example.com", "hash");
        var other = User.Create($"other-{Guid.NewGuid():N}@example.com", "hash");
        central.Users.AddRange(noisy, other);
        var first = Notification.Create(noisy.Id, "test", "{}");
        var second = Notification.Create(noisy.Id, "test", "{}");
        var victim = Notification.Create(other.Id, "test", "{}");
        central.Notifications.AddRange(first, second, victim);
        DateTime now = DateTime.UtcNow;
        // Retained legacy history has no LastAttemptOnUtc. Exercise the aggregate
        // over a substantial history, not just the three pending rows.
        for (int index = 0; index < 1000; index++) {
            var historical = Notification.Create(index % 2 == 0 ? noisy.Id : other.Id, "test", "{}");
            central.Notifications.Add(historical);
            var deliveredMessage = NotificationWebPushOutboxMessage.Create(historical.Id, now.AddDays(-5));
            deliveredMessage.MarkProcessed(now.AddDays(-4));
            central.NotificationWebPushOutbox.Add(deliveredMessage);
        }
        central.NotificationWebPushOutbox.AddRange(
            NotificationWebPushOutboxMessage.Create(first.Id, now.AddMinutes(-3)),
            NotificationWebPushOutboxMessage.Create(second.Id, now.AddMinutes(-2)),
            NotificationWebPushOutboxMessage.Create(victim.Id, now.AddMinutes(-1)));
        await central.SaveChangesAsync();
        var delivered = new List<NotificationId>();
        for (int attempt = 0; attempt < 2; attempt++) {
            DbContextOptions<FoodDiaryDbContext> options = new DbContextOptionsBuilder<FoodDiaryDbContext>()
                .UseNpgsql(central.Database.GetConnectionString()).Options;
            await using var scopeCentral = new FoodDiaryDbContext(options);
            await using ServiceProvider provider = CreateProvider(scopeCentral);
            IWebPushNotificationSender sender = provider.GetRequiredService<IWebPushNotificationSender>();
            sender.SendBatchAsync(Arg.Any<Notification>(), Arg.Any<IReadOnlySet<Guid>>(), Arg.Any<CancellationToken>())
                .Returns(call => {
                    Notification notification = call.Arg<Notification>();
                    delivered.Add(notification.Id);
                    return failFirst && notification.Id == first.Id
                        ? Task.FromException<WebPushDeliveryOutcome>(new HttpRequestException("Slow endpoint"))
                        : Task.FromResult(WebPushDeliveryOutcome.Skipped);
                });
            await provider.GetRequiredService<INotificationWebPushOutboxProcessor>().ProcessDueAsync(1);
        }

        Assert.Equal(new[] { first.Id, victim.Id }, delivered);
        Assert.NotNull((await central.NotificationWebPushOutbox.AsNoTracking()
            .SingleAsync(message => message.NotificationId == first.Id)).LastAttemptOnUtc);
        Assert.NotNull((await central.NotificationWebPushOutbox.AsNoTracking()
            .SingleAsync(message => message.NotificationId == victim.Id)).ProcessedOnUtc);
    }
}
