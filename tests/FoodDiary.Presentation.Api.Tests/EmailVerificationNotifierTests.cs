using FoodDiary.Presentation.Api.Hubs;
using FoodDiary.Presentation.Api.Services;
using Microsoft.AspNetCore.SignalR;

namespace FoodDiary.Presentation.Api.Tests;

[ExcludeFromCodeCoverage]
public sealed class EmailVerificationNotifierTests {
    [Fact]
    public async Task NotifyEmailVerifiedAsync_SendsEmailVerifiedEventToUser() {
        var hubContext = new RecordingHubContext();
        var notifier = new EmailVerificationNotifier(hubContext);
        var userId = Guid.NewGuid();
        using var cts = new CancellationTokenSource();

        await notifier.NotifyEmailVerifiedAsync(userId, cts.Token);

        Assert.Equal(userId.ToString(), hubContext.Clients.UserId);
        Assert.Equal(EmailVerificationHubMethods.EmailVerified, hubContext.Clients.ClientProxy.Method);
        Assert.Empty(hubContext.Clients.ClientProxy.Args);
        Assert.Equal(cts.Token, hubContext.Clients.ClientProxy.CancellationToken);
    }

    [ExcludeFromCodeCoverage]
    private sealed class RecordingHubContext : IHubContext<EmailVerificationHub> {
        public RecordingHubClients Clients { get; } = new();

        IHubClients IHubContext<EmailVerificationHub>.Clients => Clients;

        public IGroupManager Groups { get; } = new RecordingGroupManager();
    }

    [ExcludeFromCodeCoverage]
    private sealed class RecordingHubClients : IHubClients {
        public RecordingClientProxy ClientProxy { get; } = new();
        public string? UserId { get; private set; }

        public IClientProxy All => ClientProxy;
        public IClientProxy AllExcept(IReadOnlyList<string> excludedConnectionIds) => ClientProxy;
        public IClientProxy Client(string connectionId) => ClientProxy;
        public IClientProxy Clients(IReadOnlyList<string> connectionIds) => ClientProxy;
        public IClientProxy Group(string groupName) => ClientProxy;
        public IClientProxy GroupExcept(string groupName, IReadOnlyList<string> excludedConnectionIds) => ClientProxy;
        public IClientProxy Groups(IReadOnlyList<string> groupNames) => ClientProxy;

        public IClientProxy User(string userId) {
            UserId = userId;
            return ClientProxy;
        }

        public IClientProxy Users(IReadOnlyList<string> userIds) => ClientProxy;
    }

    [ExcludeFromCodeCoverage]
    private sealed class RecordingClientProxy : IClientProxy {
        public string? Method { get; private set; }
        public object?[] Args { get; private set; } = [];
        public CancellationToken CancellationToken { get; private set; }

        public Task SendCoreAsync(string method, object?[] args, CancellationToken cancellationToken = default) {
            Method = method;
            Args = args;
            CancellationToken = cancellationToken;
            return Task.CompletedTask;
        }
    }

    [ExcludeFromCodeCoverage]
    private sealed class RecordingGroupManager : IGroupManager {
        public Task AddToGroupAsync(string connectionId, string groupName, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task RemoveFromGroupAsync(string connectionId, string groupName, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }
}
