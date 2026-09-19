using FoodDiary.Mediator;
using FoodDiary.Modules.Notifications.Application.Commands.DeliverTestNotification;
using FoodDiary.Modules.Notifications.Application.Services;
using FoodDiary.Results;

namespace FoodDiary.Modules.Notifications.Application.Tests;

[ExcludeFromCodeCoverage]
public sealed class TestNotificationDeliveryDispatcherTests {
    [Fact]
    public async Task DispatchAsync_PropagatesFailureToSchedulerAsync() {
        ISender sender = Substitute.For<ISender>();
        sender.Send(Arg.Any<DeliverTestNotificationCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure(new Error("Delivery.Failed", "Private failure details")));
        var dispatcher = new TestNotificationDeliveryDispatcher(sender);

        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            dispatcher.DispatchAsync(Guid.NewGuid(), "test"));

        Assert.Contains("Delivery.Failed", exception.Message, StringComparison.Ordinal);
        Assert.DoesNotContain("Private failure details", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task DispatchAsync_ForwardsRequestAndCancellationAsync() {
        ISender sender = Substitute.For<ISender>();
        var userId = Guid.NewGuid();
        using var cancellation = new CancellationTokenSource();
        sender.Send(Arg.Any<DeliverTestNotificationCommand>(), Arg.Any<CancellationToken>()).Returns(Result.Success());

        await new TestNotificationDeliveryDispatcher(sender).DispatchAsync(userId, "test", cancellation.Token);

        await sender.Received(1).Send(Arg.Is<DeliverTestNotificationCommand>(request =>
            request.UserId == userId && request.Type == "test"), cancellation.Token);
    }
}
