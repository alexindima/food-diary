using FoodDiary.Domain.Entities.Notifications;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Tests.Domain;

public class NotificationInvariantTests {
    [Fact]
    public void Create_WithEmptyUserId_Throws() {
        Assert.Throws<ArgumentException>(() =>
            Notification.Create(UserId.Empty, "info", "Title"));
    }

    [Fact]
    public void Create_WithBlankType_Throws() {
        Assert.Throws<ArgumentException>(() =>
            Notification.Create(UserId.New(), "   ", "Title"));
    }

    [Fact]
    public void Create_WithBlankTitle_Throws() {
        Assert.Throws<ArgumentException>(() =>
            Notification.Create(UserId.New(), "info", "   "));
    }

    [Fact]
    public void Create_WithTooLongType_Throws() {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            Notification.Create(UserId.New(), new string('t', 65), "Title"));
    }

    [Fact]
    public void Create_WithTooLongTitle_Throws() {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            Notification.Create(UserId.New(), "info", new string('t', 257)));
    }

    [Fact]
    public void Create_WithTooLongBody_Throws() {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            Notification.Create(UserId.New(), "info", "Title", body: new string('b', 1001)));
    }

    [Fact]
    public void Create_TrimsTypeAndTitle() {
        var notification = Notification.Create(
            UserId.New(), "  info  ", "  New message  ", body: "  Hello  ");

        Assert.Equal("info", notification.Type);
        Assert.Equal("New message", notification.Title);
        Assert.Equal("Hello", notification.Body);
    }

    [Fact]
    public void Create_WithWhitespaceBody_SetsNull() {
        var notification = Notification.Create(UserId.New(), "info", "Title", body: "   ");

        Assert.Null(notification.Body);
    }

    [Fact]
    public void Create_WithNullBody_SetsNull() {
        var notification = Notification.Create(UserId.New(), "info", "Title");

        Assert.Null(notification.Body);
    }

    [Fact]
    public void Create_SetsIsReadToFalse() {
        var notification = Notification.Create(UserId.New(), "info", "Title");

        Assert.False(notification.IsRead);
        Assert.Null(notification.ReadAtUtc);
    }

    [Fact]
    public void Create_WithReferenceId_StoresIt() {
        var notification = Notification.Create(
            UserId.New(), "info", "Title", referenceId: "ref-123");

        Assert.Equal("ref-123", notification.ReferenceId);
    }

    [Fact]
    public void MarkAsRead_SetsIsReadAndTimestamp() {
        var notification = Notification.Create(UserId.New(), "info", "Title");

        notification.MarkAsRead();

        Assert.True(notification.IsRead);
        Assert.NotNull(notification.ReadAtUtc);
    }

    [Fact]
    public void MarkAsRead_WhenAlreadyRead_IsIdempotent() {
        var notification = Notification.Create(UserId.New(), "info", "Title");
        notification.MarkAsRead();
        var firstReadAt = notification.ReadAtUtc;

        notification.MarkAsRead();

        Assert.Equal(firstReadAt, notification.ReadAtUtc);
    }
}
