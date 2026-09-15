using FoodDiary.Modules.Identity.Presentation.Services;
using FoodDiary.Modules.Identity.Presentation.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace FoodDiary.Modules.Identity.Presentation.Tests;

[ExcludeFromCodeCoverage]
public sealed class EmailVerificationNotifierTests {
    [Fact]
    public async Task NotifyEmailVerifiedAsync_SendsEmailVerifiedEventToUser() {
        IHubContext<EmailVerificationHub> hubContext = Substitute.For<IHubContext<EmailVerificationHub>>();
        IHubClients clients = Substitute.For<IHubClients>();
        IClientProxy clientProxy = Substitute.For<IClientProxy>();
        hubContext.Clients.Returns(clients);
        clients.User(Arg.Any<string>()).Returns(clientProxy);
        clientProxy.SendCoreAsync(Arg.Any<string>(), Arg.Any<object?[]>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
        EmailVerificationNotifier notifier = new(hubContext);
        var userId = Guid.NewGuid();
        using var cts = new CancellationTokenSource();

        await notifier.NotifyEmailVerifiedAsync(userId, cts.Token);

        clients.Received(1).User(userId.ToString());
        await clientProxy.Received(1).SendCoreAsync(
            EmailVerificationHubMethods.EmailVerified,
            Arg.Is<object?[]>(args => args!.Length == 0),
            cts.Token);
    }
}
