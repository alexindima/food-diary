using FoodDiary.Presentation.Api.Services;
using System.Security.Claims;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Connections.Features;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging.Abstractions;

namespace FoodDiary.Presentation.Api.Tests;

[ExcludeFromCodeCoverage]
public sealed class UserIdProviderTests {
    [Fact]
    public void GetUserId_WithNullConnection_ReturnsNull() {
        var provider = new UserIdProvider();

        string? userId = provider.GetUserId(connection: null);

        Assert.Null(userId);
    }

    [Fact]
    public void GetUserId_WithValidUserClaim_ReturnsUserGuid() {
        var provider = new UserIdProvider();
        var expectedUserId = Guid.NewGuid();
        HubConnectionContext connection = CreateConnection(new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim(ClaimTypes.NameIdentifier, expectedUserId.ToString())],
            authenticationType: "test")));

        string? userId = provider.GetUserId(connection);

        Assert.Equal(expectedUserId.ToString(), userId);
    }

    [Fact]
    public void GetUserId_WithInvalidUserClaim_ReturnsNull() {
        var provider = new UserIdProvider();
        HubConnectionContext connection = CreateConnection(new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim(ClaimTypes.NameIdentifier, "not-a-guid")],
            authenticationType: "test")));

        string? userId = provider.GetUserId(connection);

        Assert.Null(userId);
    }

    private static HubConnectionContext CreateConnection(ClaimsPrincipal user) {
        var connectionContext = new DefaultConnectionContext();
        connectionContext.Features.Set<IConnectionUserFeature>(new TestConnectionUserFeature(user));

        return new HubConnectionContext(
            connectionContext,
            new HubConnectionContextOptions(),
            NullLoggerFactory.Instance);
    }

    [ExcludeFromCodeCoverage]
    private sealed class TestConnectionUserFeature(ClaimsPrincipal user) : IConnectionUserFeature {
        public ClaimsPrincipal? User { get; set; } = user;
    }
}
