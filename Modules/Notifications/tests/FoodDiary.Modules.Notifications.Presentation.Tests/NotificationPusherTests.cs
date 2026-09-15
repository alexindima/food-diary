using FoodDiary.Modules.Notifications.Presentation.Services;
using FoodDiary.Modules.Notifications.Presentation.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace FoodDiary.Modules.Notifications.Presentation.Tests;

[ExcludeFromCodeCoverage]
public sealed class NotificationPusherTests {
    [Fact]
    public async Task NotificationPusher_SendsUnreadCountAndChangedEventsToUser() {
        IHubContext<NotificationHub> hubContext = Substitute.For<IHubContext<NotificationHub>>();
        IHubClients clients = Substitute.For<IHubClients>();
        IClientProxy clientProxy = Substitute.For<IClientProxy>();
        hubContext.Clients.Returns(clients);
        clients.User(Arg.Any<string>()).Returns(clientProxy);
        clientProxy.SendCoreAsync(Arg.Any<string>(), Arg.Any<object?[]>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
        NotificationPusher pusher = new(hubContext);
        var userId = Guid.NewGuid();
        using var cts = new CancellationTokenSource();

        await pusher.PushUnreadCountAsync(userId, 7, cts.Token);
        await pusher.PushNotificationsChangedAsync(userId, cts.Token);

        clients.Received(2).User(userId.ToString());
        await clientProxy.Received(1).SendCoreAsync(
            NotificationHubMethods.UnreadCountUpdated,
            Arg.Is<object?[]>(args => args!.Length == 1 && Equals(args[0], 7)),
            cts.Token);
        await clientProxy.Received(1).SendCoreAsync(
            NotificationHubMethods.NotificationsChanged,
            Arg.Is<object?[]>(args => args!.Length == 0),
            cts.Token);
    }
}
