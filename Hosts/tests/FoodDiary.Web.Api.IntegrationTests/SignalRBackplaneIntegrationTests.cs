using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading.Channels;
using FoodDiary.Modules.Identity.Presentation.Hubs;
using FoodDiary.Modules.Notifications.Application.Abstractions.Common;
using FoodDiary.Modules.Notifications.Presentation.Hubs;
using FoodDiary.Web.Api.IntegrationTests.TestInfrastructure;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit.Sdk;

namespace FoodDiary.Web.Api.IntegrationTests;

[ExcludeFromCodeCoverage]
public sealed class SignalRBackplaneIntegrationTests {
    private static readonly Guid Alice = Guid.Parse("00000000-0000-4000-8000-000000000001");
    private static readonly Guid Bob = Guid.Parse("00000000-0000-4000-8000-000000000002");

    [SignalRRedisServer.RequiresRedisTheory]
    [InlineData("/hubs/notifications", NotificationHubMethods.UnreadCountUpdated)]
    [InlineData("/hubs/email-verification", EmailVerificationHubMethods.EmailVerified)]
    public async Task UserPush_ReachesOtherApiInstance_AndRemainsUserScopedAsync(string hubPath, string method) {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(40));
        CancellationToken cancellationToken = timeout.Token;
        await using SignalRRedisServer redis = await SignalRRedisServer.StartAsync(cancellationToken);
        var firstReady = new ConnectionReadyFilter();
        var secondReady = new ConnectionReadyFilter();
        await using var firstBase = new TransportApiWebApplicationFactory();
        await using var secondBase = new TransportApiWebApplicationFactory();
        await using WebApplicationFactory<Program> first = CreateInstance(firstBase, redis.ConnectionString, firstReady);
        await using WebApplicationFactory<Program> second = CreateInstance(secondBase, redis.ConnectionString, secondReady);
        using HttpClient firstClient = first.CreateClient();
        using HttpClient secondClient = second.CreateClient();
        using WebSocket firstAlice = await ConnectAsync(first, hubPath, Alice, firstReady, cancellationToken);
        using WebSocket firstBob = await ConnectAsync(first, hubPath, Bob, firstReady, cancellationToken);
        using WebSocket secondAlice = await ConnectAsync(second, hubPath, Alice, secondReady, cancellationToken);

        await PublishAsync(second.Services, hubPath, cancellationToken);
        await AssertInvocationAsync(firstAlice, method, cancellationToken);
        await AssertInvocationAsync(secondAlice, method, cancellationToken);

        await PublishAsync(first.Services, hubPath, cancellationToken);
        await AssertInvocationAsync(secondAlice, method, cancellationToken);
        await AssertInvocationAsync(firstAlice, method, cancellationToken);

        using var noBobMessage = new CancellationTokenSource(TimeSpan.FromMilliseconds(250));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => ReadFrameAsync(firstBob, noBobMessage.Token));
    }

    private static WebApplicationFactory<Program> CreateInstance(
        TransportApiWebApplicationFactory factory, string redis, ConnectionReadyFilter ready) =>
        factory.WithWebHostBuilder(builder => {
            builder.UseSetting("ConnectionStrings:Redis", redis);
            builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(
                new Dictionary<string, string?>(StringComparer.Ordinal) { ["ConnectionStrings:Redis"] = redis }));
            builder.ConfigureTestServices(services => services.AddSignalR(options => options.AddFilter(ready)));
        });

    private static async Task<WebSocket> ConnectAsync(
        WebApplicationFactory<Program> factory, string path, Guid userId, ConnectionReadyFilter ready, CancellationToken cancellationToken) {
        WebSocketClient client = factory.Server.CreateWebSocketClient();
        client.ConfigureRequest = request => {
            request.Headers[TestAuthenticationHandler.AuthenticateHeader] = "true";
            request.Headers[TestAuthenticationHandler.UserIdHeader] = userId.ToString();
        };
        WebSocket socket = await client.ConnectAsync(new Uri("ws://localhost" + path), cancellationToken);
        try {
            await socket.SendAsync(Encoding.UTF8.GetBytes("{\"protocol\":\"json\",\"version\":1}\u001e"),
                WebSocketMessageType.Text, endOfMessage: true, cancellationToken);
            Assert.Equal("{}\u001e", await ReadFrameAsync(socket, cancellationToken));
            await ready.Connections.Reader.ReadAsync(cancellationToken);
            return socket;
        } catch {
            socket.Dispose();
            throw;
        }
    }

    private static async Task PublishAsync(IServiceProvider services, string hubPath, CancellationToken cancellationToken) {
        await using AsyncServiceScope scope = services.CreateAsyncScope();
        if (string.Equals(hubPath, "/hubs/notifications", StringComparison.Ordinal)) {
            await scope.ServiceProvider.GetRequiredService<INotificationPusher>().PushUnreadCountAsync(Alice, 7, cancellationToken);
        } else {
            await scope.ServiceProvider.GetRequiredService<IHubContext<EmailVerificationHub>>().Clients.User(Alice.ToString())
                .SendAsync(EmailVerificationHubMethods.EmailVerified, cancellationToken);
        }
    }

    private static async Task AssertInvocationAsync(WebSocket socket, string method, CancellationToken cancellationToken) {
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        deadline.CancelAfter(TimeSpan.FromSeconds(5));
        try {
            while (true) {
                string frame = await ReadFrameAsync(socket, deadline.Token);
                foreach (string message in frame.Split('\u001e', StringSplitOptions.RemoveEmptyEntries)) {
                    using var payload = JsonDocument.Parse(message);
                    if (payload.RootElement.GetProperty("type").GetInt32() != 1) {
                        continue;
                    }
                    Assert.Equal(method, payload.RootElement.GetProperty("target").GetString());
                    JsonElement arguments = payload.RootElement.GetProperty("arguments");
                    if (string.Equals(method, NotificationHubMethods.UnreadCountUpdated, StringComparison.Ordinal)) {
                        Assert.Equal(7, Assert.Single(arguments.EnumerateArray()).GetInt32());
                    } else {
                        Assert.Empty(arguments.EnumerateArray());
                    }
                    return;
                }
            }
        } catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested) {
            throw new XunitException("A SignalR message did not reach the user connected to the other API instance.");
        }
    }

    private static async Task<string> ReadFrameAsync(WebSocket socket, CancellationToken cancellationToken) {
        byte[] buffer = new byte[4096];
        await using var content = new MemoryStream();
        WebSocketReceiveResult result;
        do {
            result = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), cancellationToken);
            Assert.Equal(WebSocketMessageType.Text, result.MessageType);
            await content.WriteAsync(buffer.AsMemory(0, result.Count), cancellationToken);
        } while (!result.EndOfMessage);
        return Encoding.UTF8.GetString(content.ToArray());
    }

    [ExcludeFromCodeCoverage]
    private sealed class ConnectionReadyFilter : IHubFilter {
        public Channel<string> Connections { get; } = Channel.CreateUnbounded<string>();

        public async Task OnConnectedAsync(HubLifetimeContext context, Func<HubLifetimeContext, Task> next) {
            await next(context);
            Connections.Writer.TryWrite(context.Context.ConnectionId);
        }
    }
}
