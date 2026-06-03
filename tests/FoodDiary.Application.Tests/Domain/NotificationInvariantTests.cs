using FoodDiary.Domain.Entities.Notifications;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Tests.Domain;

[ExcludeFromCodeCoverage]
public class NotificationInvariantTests {
    [Fact]
    public void Create_WithEmptyUserId_Throws() {
        Assert.Throws<ArgumentException>(() =>
            Notification.Create(UserId.Empty, "info", "{}"));
    }

    [Fact]
    public void Create_WithBlankType_Throws() {
        Assert.Throws<ArgumentException>(() =>
            Notification.Create(UserId.New(), "   ", "{}"));
    }

    [Fact]
    public void Create_WithBlankPayload_Throws() {
        Assert.Throws<ArgumentException>(() =>
            Notification.Create(UserId.New(), "info", "   "));
    }

    [Fact]
    public void Create_WithTooLongType_Throws() {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            Notification.Create(UserId.New(), new string('t', 65), "{}"));
    }

    [Fact]
    public void Create_WithTooLongPayload_Throws() {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            Notification.Create(UserId.New(), "info", new string('p', 4001)));
    }

    [Fact]
    public void Create_TrimsTypeAndPayload() {
        var notification = Notification.Create(
            UserId.New(), "  info  ", "  {\"kind\":\"test\"}  ");

        Assert.Equal("info", notification.Type);
        Assert.Equal("{\"kind\":\"test\"}", notification.PayloadJson);
    }

    [Fact]
    public void Create_SetsIsReadToFalse() {
        var notification = Notification.Create(UserId.New(), "info", "{}");

        Assert.False(notification.IsRead);
        Assert.Null(notification.ReadAtUtc);
    }

    [Fact]
    public void Create_WithReferenceId_StoresIt() {
        var notification = Notification.Create(
            UserId.New(), "info", "{}", referenceId: "ref-123");

        Assert.Equal("ref-123", notification.ReferenceId);
    }

    [Fact]
    public void MarkAsRead_SetsIsReadAndTimestamp() {
        var notification = Notification.Create(UserId.New(), "info", "{}");

        notification.MarkAsRead();

        Assert.True(notification.IsRead);
        Assert.NotNull(notification.ReadAtUtc);
    }

    [Fact]
    public void MarkAsRead_WhenAlreadyRead_IsIdempotent() {
        var notification = Notification.Create(UserId.New(), "info", "{}");
        notification.MarkAsRead();
        var firstReadAt = notification.ReadAtUtc;

        notification.MarkAsRead();

        Assert.Equal(firstReadAt, notification.ReadAtUtc);
    }

    [Fact]
    public void WebPushSubscription_Create_WithValidValues_NormalizesFields() {
        var userId = UserId.New();
        var expiresAtLocal = new DateTime(2026, 3, 27, 12, 30, 0, DateTimeKind.Local);

        var subscription = WebPushSubscription.Create(
            userId,
            endpoint: "  https://push.example.com/device  ",
            p256Dh: "  p256  ",
            auth: "  auth  ",
            expiresAtLocal,
            locale: "  en-US  ",
            userAgent: "  Browser  ");

        Assert.NotEqual(WebPushSubscriptionId.Empty, subscription.Id);
        Assert.Equal(userId, subscription.UserId);
        Assert.Equal("https://push.example.com/device", subscription.Endpoint);
        Assert.Equal("p256", subscription.P256Dh);
        Assert.Equal("auth", subscription.Auth);
        Assert.Equal(expiresAtLocal.ToUniversalTime(), subscription.ExpirationTimeUtc);
        Assert.Equal("en-US", subscription.Locale);
        Assert.Equal("Browser", subscription.UserAgent);
        Assert.NotEqual(default, subscription.CreatedOnUtc);
    }

    [Fact]
    public void WebPushSubscription_Create_WithBlankOptionalValues_StoresNulls() {
        var subscription = WebPushSubscription.Create(
            UserId.New(),
            "https://push.example.com/device",
            "p256",
            "auth",
            expirationTimeUtc: null,
            locale: " ",
            userAgent: " ");

        Assert.Null(subscription.ExpirationTimeUtc);
        Assert.Null(subscription.Locale);
        Assert.Null(subscription.UserAgent);
    }

    [Fact]
    public void WebPushSubscription_Refresh_ReplacesMutableFieldsAndSetsModified() {
        var initialUserId = UserId.New();
        var nextUserId = UserId.New();
        var subscription = WebPushSubscription.Create(
            initialUserId,
            "https://push.example.com/device",
            "old-p256",
            "old-auth");
        var expiresAtLocal = new DateTime(2026, 3, 28, 8, 0, 0, DateTimeKind.Local);

        subscription.Refresh(
            nextUserId,
            p256Dh: "  new-p256  ",
            auth: "  new-auth  ",
            expiresAtLocal,
            locale: "  ru  ",
            userAgent: "  Mobile  ");

        Assert.Equal(nextUserId, subscription.UserId);
        Assert.Equal("https://push.example.com/device", subscription.Endpoint);
        Assert.Equal("new-p256", subscription.P256Dh);
        Assert.Equal("new-auth", subscription.Auth);
        Assert.Equal(expiresAtLocal.ToUniversalTime(), subscription.ExpirationTimeUtc);
        Assert.Equal("ru", subscription.Locale);
        Assert.Equal("Mobile", subscription.UserAgent);
        Assert.NotNull(subscription.ModifiedOnUtc);
    }

    [Fact]
    public void WebPushSubscription_Create_WithEmptyUserId_Throws() {
        Assert.Throws<ArgumentException>(() =>
            WebPushSubscription.Create(UserId.Empty, "endpoint", "p256", "auth"));
    }

    [Theory]
    [InlineData("", "p256", "auth")]
    [InlineData("endpoint", "", "auth")]
    [InlineData("endpoint", "p256", "")]
    public void WebPushSubscription_Create_WithBlankRequiredValues_Throws(string endpoint, string p256Dh, string auth) {
        Assert.Throws<ArgumentException>(() =>
            WebPushSubscription.Create(UserId.New(), endpoint, p256Dh, auth));
    }

    [Fact]
    public void WebPushSubscription_Create_WithTooLongRequiredValues_Throws() {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            WebPushSubscription.Create(UserId.New(), new string('e', 2049), "p256", "auth"));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            WebPushSubscription.Create(UserId.New(), "endpoint", new string('p', 513), "auth"));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            WebPushSubscription.Create(UserId.New(), "endpoint", "p256", new string('a', 513)));
    }

    [Fact]
    public void WebPushSubscription_Create_WithTooLongOptionalValues_Throws() {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            WebPushSubscription.Create(UserId.New(), "endpoint", "p256", "auth", locale: new string('l', 17)));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            WebPushSubscription.Create(UserId.New(), "endpoint", "p256", "auth", userAgent: new string('u', 513)));
    }

    [Fact]
    public void WebPushSubscription_Create_WithUnspecifiedExpiration_Throws() {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            WebPushSubscription.Create(
                UserId.New(),
                "endpoint",
                "p256",
                "auth",
                new DateTime(2026, 3, 27)));
    }

    [Fact]
    public void WebPushSubscription_Refresh_WithInvalidValues_Throws() {
        var subscription = WebPushSubscription.Create(UserId.New(), "endpoint", "p256", "auth");

        Assert.Throws<ArgumentException>(() => subscription.Refresh(UserId.Empty, "p256", "auth"));
        Assert.Throws<ArgumentException>(() => subscription.Refresh(UserId.New(), " ", "auth"));
        Assert.Throws<ArgumentException>(() => subscription.Refresh(UserId.New(), "p256", " "));
        Assert.Throws<ArgumentOutOfRangeException>(() => subscription.Refresh(UserId.New(), new string('p', 513), "auth"));
        Assert.Throws<ArgumentOutOfRangeException>(() => subscription.Refresh(UserId.New(), "p256", new string('a', 513)));
        Assert.Throws<ArgumentOutOfRangeException>(() => subscription.Refresh(UserId.New(), "p256", "auth", new DateTime(2026, 3, 27)));
    }
}
