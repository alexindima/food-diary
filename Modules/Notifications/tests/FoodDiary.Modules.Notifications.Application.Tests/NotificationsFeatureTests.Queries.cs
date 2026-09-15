using FoodDiary.Application.Abstractions.Authentication.Common;
using FoodDiary.Modules.Notifications.Contracts.Common;
using FoodDiary.Modules.Users.Contracts.Common;
using FoodDiary.Modules.Users.Contracts.Models;
using FoodDiary.Modules.Notifications.Application.Common;
using FoodDiary.Modules.Notifications.Application.Queries.GetNotifications;
using FoodDiary.Modules.Notifications.Application.Services;
using FoodDiary.Modules.Notifications.Application.Queries.GetUnreadCount;
using FoodDiary.Modules.Notifications.Domain.Entities;
using FoodDiary.Modules.Users.Domain.Entities;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Results;
using FoodDiary.Modules.Notifications.Application.Models;

namespace FoodDiary.Modules.Notifications.Application.Tests;

public partial class NotificationsFeatureTests {

    [Fact]
    public async Task GetUnreadCount_ReturnsCount() {
        var userId = UserId.New();
        var repo = new InMemoryNotificationRepository();
        repo.Seed(Notification.Create(userId, "info", "{}"));
        repo.Seed(Notification.Create(userId, "info", "{}"));

        var handler = new GetUnreadCountQueryHandler(
            repo,
            CreateNotificationUserContextService(CreateUser(userId)),
            CreateNotificationUserAccessService(CreateUser(userId)));
        Result<int> result = await handler.Handle(
            new GetUnreadCountQuery(userId.Value),
            CancellationToken.None);

        ResultAssert.Success(result);
        Assert.Equal(2, result.Value);
    }

    [Fact]
    public async Task GetUnreadCount_WithNullUserId_ReturnsInvalidToken() {
        var handler = new GetUnreadCountQueryHandler(
            new InMemoryNotificationRepository(),
            CreateNotificationUserContextService(CreateUser()),
            CreateNotificationUserAccessService(CreateUser()));

        Result<int> result = await handler.Handle(new GetUnreadCountQuery(UserId: null), CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Authentication.InvalidToken", result.Error.Code);
    }

    [Fact]
    public async Task GetUnreadCount_WithPasswordUser_ExcludesPasswordSetupSuggestion() {
        var userId = UserId.New();
        User user = CreateUser(userId);
        var repo = new InMemoryNotificationRepository();
        repo.Seed(Notification.Create(userId, NotificationTypes.PasswordSetupSuggested, "{}"));
        repo.Seed(Notification.Create(userId, NotificationTypes.FastingCompleted, "{}"));
        var handler = new GetUnreadCountQueryHandler(
            repo,
            CreateNotificationUserContextService(user),
            CreateNotificationUserAccessService(user));

        Result<int> result = await handler.Handle(
            new GetUnreadCountQuery(userId.Value),
            CancellationToken.None);

        ResultAssert.Success(result);
        Assert.Equal(1, result.Value);
    }

    [Fact]
    public async Task GetUnreadCount_WhenUserDeleted_ReturnsFailure() {
        var userId = UserId.New();
        var repo = new InMemoryNotificationRepository();
        repo.Seed(Notification.Create(userId, "info", "{}"));
        var handler = new GetUnreadCountQueryHandler(
            repo,
            CreateNotificationUserContextService(CreateDeletedUser(userId)),
            CreateNotificationUserAccessService(CreateDeletedUser(userId)));

        Result<int> result = await handler.Handle(
            new GetUnreadCountQuery(userId.Value),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Authentication.AccountDeleted", result.Error.Code);
    }

    [Fact]
    public async Task NotificationUserContextService_WhenUserMissing_ReturnsAccessFailure() {
        IUserNotificationProfileService userProfileService = Substitute.For<IUserNotificationProfileService>();
        userProfileService
            .GetAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result.Failure<UserNotificationProfileModel>(AuthenticationErrors.InvalidToken)));
        var service = new NotificationUserContextService(userProfileService);

        Result<NotificationUserContext> result = await service.GetAsync(UserId.New(), CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Authentication.InvalidToken", result.Error.Code);
    }

    [Fact]
    public async Task GetNotifications_WithEmptyUserId_ReturnsInvalidToken() {
        var handler = new GetNotificationsQueryHandler(
            CreateNotificationUserContextService(CreateUser()),
            new InMemoryNotificationRepository(), new RecordingNotificationTextRenderer(),
            CreateNotificationUserAccessService(CreateUser()));

        Result<IReadOnlyList<NotificationModel>> result = await handler.Handle(new GetNotificationsQuery(Guid.Empty), CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Authentication.InvalidToken", result.Error.Code);
    }

    [Fact]
    public async Task GetNotifications_WhenUserDeleted_ReturnsAccessFailure() {
        var userId = UserId.New();
        var handler = new GetNotificationsQueryHandler(
            CreateNotificationUserContextService(CreateDeletedUser(userId)),
            new InMemoryNotificationRepository(), new RecordingNotificationTextRenderer(),
            CreateNotificationUserAccessService(CreateDeletedUser(userId)));

        Result<IReadOnlyList<NotificationModel>> result = await handler.Handle(new GetNotificationsQuery(userId.Value), CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Authentication.AccountDeleted", result.Error.Code);
    }

    [Fact]
    public async Task GetNotifications_WhenContextFailsAfterAccessCheck_ReturnsFailure() {
        var userId = UserId.New();
        INotificationUserContextService contextService = Substitute.For<INotificationUserContextService>();
        contextService
            .GetAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result.Failure<NotificationUserContext>(AuthenticationErrors.InvalidToken)));
        var handler = new GetNotificationsQueryHandler(
            contextService,
            new InMemoryNotificationRepository(), new RecordingNotificationTextRenderer(),
            CreateNotificationUserAccessService(CreateUser(userId)));

        Result<IReadOnlyList<NotificationModel>> result = await handler.Handle(new GetNotificationsQuery(userId.Value), CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Authentication.InvalidToken", result.Error.Code);
    }

    [Fact]
    public async Task GetNotifications_WithPasswordUser_HidesPasswordSetupSuggestion() {
        var user = User.Create("password-notifications@example.com", "hash");
        var repo = new InMemoryNotificationRepository();
        repo.Seed(Notification.Create(user.Id, NotificationTypes.PasswordSetupSuggested, "{}"));
        repo.Seed(Notification.Create(user.Id, NotificationTypes.FastingCompleted, "{}"));
        var renderer = new RecordingNotificationTextRenderer();
        var handler = new GetNotificationsQueryHandler(
            CreateNotificationUserContextService(user),
            repo, renderer,
            CreateNotificationUserAccessService(user));

        Result<IReadOnlyList<NotificationModel>> result = await handler.Handle(new GetNotificationsQuery(user.Id.Value), CancellationToken.None);

        ResultAssert.Success(result);
        Assert.Single(result.Value);
        Assert.Equal(NotificationTypes.FastingCompleted, result.Value[0].Type);
        Assert.Equal([NotificationTypes.FastingCompleted], renderer.RenderedTypes);
    }

    [Fact]
    public async Task GetUnreadCount_WhenContextFailsAfterAccessCheck_ReturnsFailure() {
        var userId = UserId.New();
        INotificationUserContextService contextService = Substitute.For<INotificationUserContextService>();
        contextService
            .GetAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result.Failure<NotificationUserContext>(AuthenticationErrors.InvalidToken)));
        var handler = new GetUnreadCountQueryHandler(
            new InMemoryNotificationRepository(),
            contextService,
            CreateNotificationUserAccessService(CreateUser(userId)));

        Result<int> result = await handler.Handle(new GetUnreadCountQuery(userId.Value), CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Authentication.InvalidToken", result.Error.Code);
    }
}
