using FoodDiary.Domain.Entities.Notifications;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Tests.Domain;

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
}
