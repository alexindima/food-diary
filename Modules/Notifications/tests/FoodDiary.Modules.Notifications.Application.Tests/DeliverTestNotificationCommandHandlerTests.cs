using FoodDiary.Application.Abstractions.Common.Abstractions.Persistence;
using FoodDiary.Modules.Notifications.Contracts.Common;
using FoodDiary.Modules.Notifications.Application.Commands.DeliverTestNotification;
using FoodDiary.Results;
using FoodDiary.Mediator;
using FoodDiary.Modules.Notifications.Application.Services;

namespace FoodDiary.Modules.Notifications.Application.Tests;

[ExcludeFromCodeCoverage]
public sealed class DeliverTestNotificationCommandHandlerTests {
    [Fact]
    public async Task Dispatcher_SendsDeliveryCommand() {
        IRequest<Result>? sent = null;
        ISender sender = Substitute.For<ISender>();
        sender.Send(Arg.Any<IRequest<Result>>(), Arg.Any<CancellationToken>())
            .Returns(call => {
                sent = call.Arg<IRequest<Result>>();
                return Task.FromResult(Result.Success());
            });
        var dispatcher = new TestNotificationDeliveryDispatcher(sender);
        var userId = Guid.NewGuid();

        await dispatcher.DispatchAsync(userId, NotificationTypes.FastingCompleted, CancellationToken.None);

        DeliverTestNotificationCommand command = Assert.IsType<DeliverTestNotificationCommand>(sent);
        Assert.Multiple(
            () => Assert.Equal(userId, command.UserId),
            () => Assert.Equal(NotificationTypes.FastingCompleted, command.Type));
    }

    [Fact]
    public async Task Handle_WithEmptyUserId_ReturnsFailureWithoutWriting() {
        INotificationWriter writer = Substitute.For<INotificationWriter>();
        var handler = new DeliverTestNotificationCommandHandler(
            writer,
            Substitute.For<INotificationClientRefreshService>(),
            Substitute.For<IUnitOfWork>(),
            Substitute.For<IPostCommitActionQueue>());

        Result result = await handler.Handle(
            new DeliverTestNotificationCommand(Guid.Empty, NotificationTypes.FastingCompleted),
            CancellationToken.None);

        ResultAssert.Failure(result);
        await writer.DidNotReceiveWithAnyArgs().AddAsync(default!, default, default);
    }

    [Theory]
    [InlineData(NotificationTypes.FastingCompleted)]
    [InlineData(NotificationTypes.FastingCheckInReminder)]
    [InlineData(NotificationTypes.EatingWindowStarted)]
    [InlineData(NotificationTypes.FastingWindowStarted)]
    [InlineData("unsupported")]
    public async Task Handle_CreatesAndPersistsExpectedNotification(string type) {
        NotificationRequest? notification = null;
        INotificationWriter writer = Substitute.For<INotificationWriter>();
        writer.AddAsync(Arg.Do<NotificationRequest>(value => notification = value), sendWebPush: true, Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
        INotificationClientRefreshService refresh = Substitute.For<INotificationClientRefreshService>();
        IUnitOfWork unitOfWork = Substitute.For<IUnitOfWork>();
        IPostCommitActionQueue postCommit = Substitute.For<IPostCommitActionQueue>();
        var handler = new DeliverTestNotificationCommandHandler(writer, refresh, unitOfWork, postCommit);
        var userId = Guid.NewGuid();

        Result result = await handler.Handle(new DeliverTestNotificationCommand(userId, type), CancellationToken.None);

        ResultAssert.Success(result);
        Assert.NotNull(notification);
        Assert.Equal(string.Equals(type, "unsupported", StringComparison.Ordinal) ? NotificationTypes.FastingCompleted : type, notification.Type);
        Assert.Equal(userId, notification.UserId.Value);
        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        await refresh.Received(1).RefreshAsync(notification.UserId, pushChanged: true, Arg.Any<CancellationToken>());
        await postCommit.Received(1).FlushAsync(Arg.Any<CancellationToken>());
    }
}
