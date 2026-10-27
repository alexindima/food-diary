using FoodDiary.Modules.Notifications.Application.Abstractions.Common;
using FoodDiary.Modules.Notifications.Application.Services;
using FoodDiary.Modules.Notifications.Contracts.Common;
using FoodDiary.Modules.Notifications.Domain.Entities;
using FoodDiary.Modules.Users.Contracts.Common;
using FoodDiary.Modules.Users.Contracts.Models;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Results;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Modules.Notifications.Application.Tests;

[ExcludeFromCodeCoverage]
public sealed class NotificationWriterDeliveryEligibilityTests {
    [Theory]
    [InlineData(true, true, 0, false)]
    [InlineData(false, true, 1, false)]
    [InlineData(true, false, 1, false)]
    [InlineData(true, true, 2, false)]
    [InlineData(true, true, 1, true)]
    [InlineData(true, true, 3, true)]
    public async Task AddAsync_PreservesFeedAndEnqueuesOnlyForEligibleDevices(
        bool accountEnabled, bool categoryEnabled, int subscriptionState, bool expectedPush) {
        var userId = UserId.New();
        var now = new DateTimeOffset(2026, 10, 27, 12, 0, 0, TimeSpan.Zero);
        using var cancellation = new CancellationTokenSource();
        Notification? feedNotification = null;
        INotificationWriteRepository repository = Substitute.For<INotificationWriteRepository>();
        repository.AddAsync(Arg.Any<Notification>(), cancellation.Token).Returns(call => {
            feedNotification = call.Arg<Notification>();
            return Task.FromResult(feedNotification);
        });
        INotificationWebPushOutbox outbox = Substitute.For<INotificationWebPushOutbox>();
        IWebPushSubscriptionReadRepository reader = Substitute.For<IWebPushSubscriptionReadRepository>();
        IWebPushSubscriptionWriteRepository writer = Substitute.For<IWebPushSubscriptionWriteRepository>();
        IUserNotificationProfileService profiles = Substitute.For<IUserNotificationProfileService>();
        profiles.GetAsync(userId, cancellation.Token).Returns(Result.Success(new UserNotificationProfileModel(
            userId, HasPassword: true, Language: "en", PushNotificationsEnabled: accountEnabled,
            FastingPushNotificationsEnabled: categoryEnabled, SocialPushNotificationsEnabled: true,
            FastingCheckInReminderHours: 4, FastingCheckInFollowUpReminderHours: 8)));
        var subscriptions = new List<WebPushSubscription>();
        if (subscriptionState is 1 or 3) {
            subscriptions.Add(WebPushSubscription.Create(userId, "https://push.example.com/active", "key", "auth", now.AddHours(1).UtcDateTime));
        }
        if (subscriptionState is 2 or 3) {
            subscriptions.Add(WebPushSubscription.Create(userId, "https://push.example.com/expired", "key", "auth", now.UtcDateTime));
        }
        reader.GetByUserAsync(userId, cancellation.Token).Returns(subscriptions);
        IServiceProvider provider = Substitute.For<IServiceProvider>();
        provider.GetService(typeof(INotificationWriteRepository)).Returns(repository);
        provider.GetService(typeof(INotificationWebPushOutbox)).Returns(outbox);
        provider.GetService(typeof(IWebPushDeliveryAudienceService)).Returns(new WebPushDeliveryAudienceService(reader, writer, profiles));
        provider.GetService(typeof(TimeProvider)).Returns(new FixedTimeProvider(now));
        NotificationWriter notificationWriter = ActivatorUtilities.CreateInstance<NotificationWriter>(provider);

        await notificationWriter.AddAsync(new NotificationRequest(userId, NotificationTypes.FastingCompleted, "{}"),
            sendWebPush: true, cancellation.Token);

        Assert.NotNull(feedNotification);
        Assert.Equal(userId, feedNotification.UserId);
        await repository.Received(1).AddAsync(feedNotification, cancellation.Token);
        if (expectedPush) {
            await outbox.Received(1).EnqueueAsync(feedNotification.Id, cancellation.Token);
        } else {
            Assert.Empty(outbox.ReceivedCalls());
        }
    }

    [ExcludeFromCodeCoverage]
    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
