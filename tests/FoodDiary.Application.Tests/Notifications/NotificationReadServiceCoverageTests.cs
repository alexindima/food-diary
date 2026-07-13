using FoodDiary.Application.Abstractions.Notifications.Common;
using FoodDiary.Application.Abstractions.Notifications.Models;
using FoodDiary.Application.Notifications.Models;
using FoodDiary.Application.Notifications.Services;
using FoodDiary.Domain.Entities.Notifications;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.ValueObjects;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Tests.Notifications;

[ExcludeFromCodeCoverage]
public sealed class NotificationReadServiceCoverageTests {
    [Fact]
    public async Task WebPushSubscriptionReadService_GetSubscriptionsAsync_MapsEveryReadModel() {
        var userId = UserId.New();
        var createdAtUtc = new DateTime(2026, 7, 13, 8, 0, 0, DateTimeKind.Utc);
        IReadOnlyList<WebPushSubscriptionReadModel> readModels = [
            new(
                "https://push.example.com/subscriptions/one",
                createdAtUtc.AddDays(2),
                "ru",
                "Firefox",
                createdAtUtc,
                createdAtUtc.AddHours(1)),
        ];
        IWebPushSubscriptionReadModelRepository repository = Substitute.For<IWebPushSubscriptionReadModelRepository>();
        repository.GetByUserReadModelsAsync(userId, Arg.Any<CancellationToken>()).Returns(readModels);
        var service = new WebPushSubscriptionReadService(repository);

        IReadOnlyList<WebPushSubscriptionModel> result = await service.GetSubscriptionsAsync(userId, CancellationToken.None);

        WebPushSubscriptionModel subscription = Assert.Single(result);
        Assert.Multiple(
            () => Assert.Equal("push.example.com", subscription.EndpointHost),
            () => Assert.Equal("ru", subscription.Locale),
            () => Assert.Equal("Firefox", subscription.UserAgent),
            () => Assert.Equal(createdAtUtc, subscription.CreatedAtUtc));
    }

    [Fact]
    public async Task WebPushDeliveryAudienceService_RemoveInvalidSubscriptionsAsync_WithEmptyIds_DoesNotReadOrDelete() {
        IWebPushSubscriptionReadRepository reader = Substitute.For<IWebPushSubscriptionReadRepository>();
        IWebPushSubscriptionWriteRepository writer = Substitute.For<IWebPushSubscriptionWriteRepository>();
        var service = new WebPushDeliveryAudienceService(
            reader,
            writer,
            Substitute.For<FoodDiary.Application.Abstractions.Users.Common.IUserDirectoryService>());

        await service.RemoveInvalidSubscriptionsAsync(UserId.New(), [], CancellationToken.None);

        await reader.DidNotReceive().GetByUserAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>());
        await writer.DidNotReceive().DeleteRangeAsync(
            Arg.Any<IReadOnlyCollection<FoodDiary.Domain.Entities.Notifications.WebPushSubscription>>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task WebPushDeliveryAudienceService_GetActiveAudienceAsync_RemovesExpiredAndMapsActiveSubscription() {
        var user = User.Create("push-audience@example.com", "hash");
        user.UpdatePreferences(new UserPreferenceUpdate(
            PushNotificationsEnabled: true,
            FastingPushNotificationsEnabled: true,
            SocialPushNotificationsEnabled: true));
        var utcNow = new DateTime(2026, 7, 13, 9, 0, 0, DateTimeKind.Utc);
        var active = WebPushSubscription.Create(
            user.Id, "https://push.example.com/active", "p256", "auth", utcNow.AddHours(1), "en");
        var expired = WebPushSubscription.Create(
            user.Id, "https://push.example.com/expired", "old", "old-auth", utcNow, "ru");
        IWebPushSubscriptionReadRepository reader = Substitute.For<IWebPushSubscriptionReadRepository>();
        IWebPushSubscriptionWriteRepository writer = Substitute.For<IWebPushSubscriptionWriteRepository>();
        FoodDiary.Application.Abstractions.Users.Common.IUserDirectoryService users =
            Substitute.For<FoodDiary.Application.Abstractions.Users.Common.IUserDirectoryService>();
        users.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);
        reader.GetByUserAsync(user.Id, Arg.Any<CancellationToken>()).Returns([active, expired]);
        var service = new WebPushDeliveryAudienceService(reader, writer, users);

        IReadOnlyList<WebPushDeliverySubscription> result = await service.GetActiveAudienceAsync(
            user.Id,
            NotificationTypes.FastingCompleted,
            utcNow,
            CancellationToken.None);

        WebPushDeliverySubscription subscription = Assert.Single(result);
        Assert.Equal(active.Id.Value, subscription.Id);
        await writer.Received(1).DeleteRangeAsync(
            Arg.Is<IReadOnlyCollection<WebPushSubscription>>(items => items.Count == 1 && items.Contains(expired)),
            CancellationToken.None);
    }

    [Fact]
    public async Task WebPushDeliveryAudienceService_GetActiveAudienceAsync_WhenUserMissing_ReturnsEmptyWithoutReadingSubscriptions() {
        IWebPushSubscriptionReadRepository reader = Substitute.For<IWebPushSubscriptionReadRepository>();
        var service = new WebPushDeliveryAudienceService(
            reader,
            Substitute.For<IWebPushSubscriptionWriteRepository>(),
            Substitute.For<FoodDiary.Application.Abstractions.Users.Common.IUserDirectoryService>());

        IReadOnlyList<WebPushDeliverySubscription> result = await service.GetActiveAudienceAsync(
            UserId.New(),
            NotificationTypes.NewComment,
            DateTime.UtcNow,
            CancellationToken.None);

        Assert.Empty(result);
        await reader.DidNotReceive().GetByUserAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task WebPushDeliveryAudienceService_GetActiveAudienceAsync_WhenCategoryDisabled_ReturnsEmpty() {
        var user = User.Create("push-disabled@example.com", "hash");
        user.UpdatePreferences(new UserPreferenceUpdate(
            PushNotificationsEnabled: true,
            FastingPushNotificationsEnabled: false));
        IWebPushSubscriptionReadRepository reader = Substitute.For<IWebPushSubscriptionReadRepository>();
        FoodDiary.Application.Abstractions.Users.Common.IUserDirectoryService users =
            Substitute.For<FoodDiary.Application.Abstractions.Users.Common.IUserDirectoryService>();
        users.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);
        var service = new WebPushDeliveryAudienceService(
            reader,
            Substitute.For<IWebPushSubscriptionWriteRepository>(),
            users);

        IReadOnlyList<WebPushDeliverySubscription> result = await service.GetActiveAudienceAsync(
            user.Id,
            NotificationTypes.FastingWindowStarted,
            DateTime.UtcNow,
            CancellationToken.None);

        Assert.Empty(result);
        await reader.DidNotReceive().GetByUserAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task WebPushDeliveryAudienceService_RemoveInvalidSubscriptionsAsync_DeletesOnlyMatchingIds() {
        var userId = UserId.New();
        var matching = WebPushSubscription.Create(userId, "https://push.example.com/matching", "p", "a");
        var other = WebPushSubscription.Create(userId, "https://push.example.com/other", "p", "a");
        IWebPushSubscriptionReadRepository reader = Substitute.For<IWebPushSubscriptionReadRepository>();
        IWebPushSubscriptionWriteRepository writer = Substitute.For<IWebPushSubscriptionWriteRepository>();
        reader.GetByUserAsync(userId, Arg.Any<CancellationToken>()).Returns([matching, other]);
        var service = new WebPushDeliveryAudienceService(
            reader,
            writer,
            Substitute.For<FoodDiary.Application.Abstractions.Users.Common.IUserDirectoryService>());

        await service.RemoveInvalidSubscriptionsAsync(userId, [matching.Id.Value, Guid.NewGuid()], CancellationToken.None);

        await writer.Received(1).DeleteRangeAsync(
            Arg.Is<IReadOnlyCollection<WebPushSubscription>>(items => items.Count == 1 && items.Contains(matching)),
            CancellationToken.None);
    }

    [Theory]
    [InlineData(NotificationTypes.FastingCompleted)]
    [InlineData(NotificationTypes.EatingWindowStarted)]
    [InlineData(NotificationTypes.FastingWindowStarted)]
    [InlineData(NotificationTypes.FastingCheckInReminder)]
    [InlineData(NotificationTypes.DietologistInvitationReceived)]
    [InlineData(NotificationTypes.DietologistInvitationAccepted)]
    [InlineData(NotificationTypes.DietologistInvitationDeclined)]
    [InlineData(NotificationTypes.NewRecommendation)]
    [InlineData(NotificationTypes.NewComment)]
    [InlineData("UnknownType")]
    public void WebPushDeliveryAudienceService_IsCategoryEnabled_CoversEveryNotificationCategory(string notificationType) {
        var user = User.Create("push-category@example.com", "hash");
        user.UpdatePreferences(new UserPreferenceUpdate(
            PushNotificationsEnabled: true,
            FastingPushNotificationsEnabled: true,
            SocialPushNotificationsEnabled: true));

        bool result = WebPushDeliveryAudienceService.IsCategoryEnabled(user, notificationType);

        Assert.True(result);
    }
}
