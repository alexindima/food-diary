using System.Net;
using System.Net.Http.Json;
using System.Net.WebSockets;
using System.Text;
using FoodDiary.Modules.Identity.Presentation.Features.Auth.Requests;
using FoodDiary.Web.Api.IntegrationTests.TestInfrastructure;
using Microsoft.AspNetCore.TestHost;

namespace FoodDiary.Web.Api.IntegrationTests;

[ExcludeFromCodeCoverage]
public sealed class SessionRealtimeRevocationApiTests(ApiWebApplicationFactory factory) : IClassFixture<ApiWebApplicationFactory> {
    [RequiresDockerFact]
    public async Task Logout_DisconnectsAnAlreadyOpenNotificationSocket() {
        using HttpClient browser = factory.CreateClient();
        using HttpResponseMessage registered = await browser.PostAsJsonAsync("/api/v1/auth/register",
            new RegisterHttpRequest($"realtime-{Guid.NewGuid():N}@example.com", "Password123!", "en"));
        registered.EnsureSuccessStatusCode();
        string cookie = AuthenticationResponseCookies.ReadBrowserSession(registered);
        AuthenticationResponseCookies.ApplyBrowserSession(browser, cookie);
        WebSocketClient client = factory.Server.CreateWebSocketClient();
        client.ConfigureRequest = request => request.Headers.Cookie = cookie;
        using WebSocket socket = await client.ConnectAsync(new Uri("ws://localhost/hubs/notifications"), CancellationToken.None);
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(20));
        await socket.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes("{\"protocol\":\"json\",\"version\":1}\u001e")),
            WebSocketMessageType.Text, endOfMessage: true, timeout.Token);
        byte[] buffer = new byte[1024];
        WebSocketReceiveResult handshake = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), timeout.Token);
        Assert.StartsWith("{}", Encoding.UTF8.GetString(buffer, 0, handshake.Count), StringComparison.Ordinal);
        using HttpResponseMessage logout = await browser.PostAsJsonAsync("/api/v1/auth/logout", new { });
        Assert.Equal(HttpStatusCode.NoContent, logout.StatusCode);
        bool closed = false;
        try {
            while (!timeout.IsCancellationRequested) {
                WebSocketReceiveResult message = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), timeout.Token);
                if (message.MessageType == WebSocketMessageType.Close ||
                    Encoding.UTF8.GetString(buffer, 0, message.Count).Contains("\"type\":7", StringComparison.Ordinal)) {
                    closed = true;
                    break;
                }
            }
        } catch (WebSocketException) when (!timeout.IsCancellationRequested) {
            closed = true;
        } catch (OperationCanceledException) when (timeout.IsCancellationRequested) {
            // A quiet, still-authorized socket must not be mistaken for a successful disconnect.
        } finally {
            socket.Abort();
        }
        Assert.True(closed, "Revoked sessions must disconnect active SignalR transports within 20 seconds.");
    }
}
