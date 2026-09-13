using FoodDiary.Application.Abstractions.Notifications.Common;
using FoodDiary.Application.Notifications.Services;
using FoodDiary.Domain.Entities.Notifications;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Tests.Notifications;

[ExcludeFromCodeCoverage]
public sealed class NotificationWriterTests {
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task AddAsync_CreatesOwnedNotificationAndEnqueuesSameIdAfterAdding(bool sendWebPush) {
        using var cancellation = new CancellationTokenSource();
        Notification? saved = null;
        var calls = new List<string>();
        INotificationWriteRepository repository = Substitute.For<INotificationWriteRepository>();
        INotificationWebPushOutbox outbox = Substitute.For<INotificationWebPushOutbox>();
        repository.AddAsync(Arg.Any<Notification>(), cancellation.Token).Returns(call => {
            saved = call.Arg<Notification>();
            calls.Add("add");
            return Task.FromResult(saved);
        });
        outbox.EnqueueAsync(Arg.Any<NotificationId>(), cancellation.Token).Returns(call => {
            Assert.NotNull(saved);
            Assert.Equal(saved.Id, call.Arg<NotificationId>());
            calls.Add("outbox");
            return Task.CompletedTask;
        });
        var request = new NotificationRequest(UserId.New(), " info ", "{}", " reference ");

        await new NotificationWriter(repository, outbox).AddAsync(request, sendWebPush, cancellation.Token);

        Assert.NotNull(saved);
        Assert.Multiple(
            () => Assert.Equal(request.UserId, saved.UserId),
            () => Assert.Equal("info", saved.Type),
            () => Assert.Equal(request.PayloadJson, saved.PayloadJson),
            () => Assert.Equal("reference", saved.ReferenceId),
            () => Assert.False(saved.IsRead),
            () => Assert.Equal(sendWebPush ? new[] { "add", "outbox" } : ["add"], calls, StringComparer.Ordinal));
        if (!sendWebPush) {
            await outbox.DidNotReceiveWithAnyArgs().EnqueueAsync(default, default);
        }
    }

    [Fact]
    public async Task AddAsync_RepositoryFailureDoesNotEnqueuePush() {
        INotificationWriteRepository repository = Substitute.For<INotificationWriteRepository>();
        INotificationWebPushOutbox outbox = Substitute.For<INotificationWebPushOutbox>();
        repository.AddAsync(Arg.Any<Notification>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<Notification>(new InvalidOperationException("write failed")));
        var writer = new NotificationWriter(repository, outbox);

        await Assert.ThrowsAsync<InvalidOperationException>(() => writer.AddAsync(
            new NotificationRequest(UserId.New(), "info", "{}"), sendWebPush: true, CancellationToken.None));

        await outbox.DidNotReceiveWithAnyArgs().EnqueueAsync(default, default);
    }

    [Fact]
    public async Task AddAsync_InvalidRequestDoesNotWriteOrEnqueue() {
        INotificationWriteRepository repository = Substitute.For<INotificationWriteRepository>();
        INotificationWebPushOutbox outbox = Substitute.For<INotificationWebPushOutbox>();
        var writer = new NotificationWriter(repository, outbox);

        await Assert.ThrowsAsync<ArgumentException>(() => writer.AddAsync(
            new NotificationRequest(UserId.Empty, "info", "{}"), sendWebPush: true, CancellationToken.None));

        await repository.DidNotReceiveWithAnyArgs().AddAsync(default!, default);
        await outbox.DidNotReceiveWithAnyArgs().EnqueueAsync(default, default);
    }
}
