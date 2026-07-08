using FoodDiary.Application.Notifications.Commands.MarkAllNotificationsRead;
using FoodDiary.Application.Notifications.Commands.MarkNotificationRead;
using FoodDiary.Domain.Entities.Notifications;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Results;

namespace FoodDiary.Application.Tests.Notifications;

public partial class NotificationsFeatureTests {

    [Fact]
    public async Task MarkNotificationRead_WithValidOwnership_Succeeds() {
        var userId = UserId.New();
        var notification = Notification.Create(userId, "info", "{}");
        var repo = new InMemoryNotificationRepository();
        repo.Seed(notification);
        var pusher = new RecordingNotificationPusher();

        RecordingPostCommitActionQueue postCommitActionQueue = CreatePostCommitActionQueue();
        var handler = new MarkNotificationReadCommandHandler(repo, repo, CreateCurrentUserAccessService(CreateUser(userId)), pusher, postCommitActionQueue);
        Result result = await handler.Handle(
            new MarkNotificationReadCommand(userId.Value, notification.Id.Value),
            CancellationToken.None);

        ResultAssert.Success(result);
        Assert.True(notification.IsRead);
        Assert.Null(pusher.UnreadCountUserId);
        Assert.True(postCommitActionQueue.HasActions);

        await postCommitActionQueue.FlushAsync();

        Assert.Equal(userId.Value, pusher.UnreadCountUserId);
        Assert.Equal(0, pusher.UnreadCount);
        Assert.Equal(userId.Value, pusher.NotificationsChangedUserId);
    }

    [Fact]
    public async Task MarkNotificationRead_WhenNotOwned_ReturnsFailure() {
        var ownerId = UserId.New();
        var otherUserId = UserId.New();
        var notification = Notification.Create(ownerId, "info", "{}");
        var repo = new InMemoryNotificationRepository();
        repo.Seed(notification);

        var handler = new MarkNotificationReadCommandHandler(repo, repo, CreateCurrentUserAccessService(CreateUser(otherUserId)), new RecordingNotificationPusher(), CreatePostCommitActionQueue());
        Result result = await handler.Handle(
            new MarkNotificationReadCommand(otherUserId.Value, notification.Id.Value),
            CancellationToken.None);

        ResultAssert.Failure(result);
    }

    [Fact]
    public async Task MarkNotificationRead_WhenNotFound_ReturnsFailure() {
        var repo = new InMemoryNotificationRepository();
        var userId = UserId.New();
        var handler = new MarkNotificationReadCommandHandler(repo, repo, CreateCurrentUserAccessService(CreateUser(userId)), new RecordingNotificationPusher(), CreatePostCommitActionQueue());

        Result result = await handler.Handle(
            new MarkNotificationReadCommand(userId.Value, Guid.NewGuid()),
            CancellationToken.None);

        ResultAssert.Failure(result);
    }

    [Fact]
    public async Task MarkNotificationRead_WithNullUserId_ReturnsFailure() {
        var handler = new MarkNotificationReadCommandHandler(
            new InMemoryNotificationRepository(),
            new InMemoryNotificationRepository(),
            CreateCurrentUserAccessService(CreateUser()),
            new RecordingNotificationPusher(),
            CreatePostCommitActionQueue());

        Result result = await handler.Handle(
            new MarkNotificationReadCommand(UserId: null, Guid.NewGuid()),
            CancellationToken.None);

        ResultAssert.Failure(result);
    }

    [Fact]
    public async Task MarkNotificationRead_WithEmptyNotificationId_ReturnsValidationFailure() {
        var userId = UserId.New();
        var repo = new InMemoryNotificationRepository();
        var handler = new MarkNotificationReadCommandHandler(
            repo,
            repo,
            CreateCurrentUserAccessService(CreateUser(userId)),
            new RecordingNotificationPusher(),
            CreatePostCommitActionQueue());

        Result result = await handler.Handle(
            new MarkNotificationReadCommand(userId.Value, Guid.Empty),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Validation.Invalid", result.Error.Code);
        Assert.Contains("NotificationId", result.Error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task MarkNotificationRead_WhenUserDeleted_ReturnsFailure() {
        var userId = UserId.New();
        var notification = Notification.Create(userId, "info", "{}");
        var repo = new InMemoryNotificationRepository();
        repo.Seed(notification);
        var handler = new MarkNotificationReadCommandHandler(repo, repo, CreateCurrentUserAccessService(CreateDeletedUser(userId)), new RecordingNotificationPusher(), CreatePostCommitActionQueue());

        Result result = await handler.Handle(
            new MarkNotificationReadCommand(userId.Value, notification.Id.Value),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Authentication.AccountDeleted", result.Error.Code);
        Assert.False(notification.IsRead);
    }

    [Fact]
    public async Task MarkAllNotificationsRead_WithValidUserId_Succeeds() {
        var userId = UserId.New();
        var repo = new InMemoryNotificationRepository();
        repo.Seed(Notification.Create(userId, "info", "{}"));
        repo.Seed(Notification.Create(userId, "info", "{}"));
        var pusher = new RecordingNotificationPusher();
        RecordingPostCommitActionQueue postCommitActionQueue = CreatePostCommitActionQueue();
        var handler = new MarkAllNotificationsReadCommandHandler(repo, repo, CreateCurrentUserAccessService(CreateUser(userId)), pusher, postCommitActionQueue);

        Result result = await handler.Handle(
            new MarkAllNotificationsReadCommand(userId.Value),
            CancellationToken.None);

        ResultAssert.Success(result);
        Assert.True(repo.MarkAllReadCalled);
        Assert.Null(pusher.UnreadCountUserId);
        Assert.True(postCommitActionQueue.HasActions);

        await postCommitActionQueue.FlushAsync();

        Assert.Equal(userId.Value, pusher.UnreadCountUserId);
        Assert.Equal(0, pusher.UnreadCount);
        Assert.Equal(userId.Value, pusher.NotificationsChangedUserId);
    }

    [Fact]
    public async Task MarkAllNotificationsRead_WithNullUserId_ReturnsFailure() {
        var handler = new MarkAllNotificationsReadCommandHandler(
            new InMemoryNotificationRepository(),
            new InMemoryNotificationRepository(),
            CreateCurrentUserAccessService(CreateUser()),
            new RecordingNotificationPusher(),
            CreatePostCommitActionQueue());

        Result result = await handler.Handle(
            new MarkAllNotificationsReadCommand(UserId: null),
            CancellationToken.None);

        ResultAssert.Failure(result);
    }

    [Fact]
    public async Task MarkAllNotificationsRead_WhenUserDeleted_ReturnsFailure() {
        var userId = UserId.New();
        var repo = new InMemoryNotificationRepository();
        var handler = new MarkAllNotificationsReadCommandHandler(repo, repo, CreateCurrentUserAccessService(CreateDeletedUser(userId)), new RecordingNotificationPusher(), CreatePostCommitActionQueue());

        Result result = await handler.Handle(
            new MarkAllNotificationsReadCommand(userId.Value),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Authentication.AccountDeleted", result.Error.Code);
        Assert.False(repo.MarkAllReadCalled);
    }
}
