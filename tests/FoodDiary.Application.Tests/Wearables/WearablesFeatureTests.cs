using FoodDiary.Application.Wearables.Commands.ConnectWearable;
using FoodDiary.Application.Wearables.Commands.DisconnectWearable;
using FoodDiary.Application.Wearables.Common;
using FoodDiary.Application.Wearables.Models;
using FoodDiary.Domain.Entities.Wearables;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Tests.Wearables;

public class WearablesFeatureTests {
    [Fact]
    public async Task ConnectWearable_WithValidCode_CreatesConnection() {
        var userId = UserId.New();
        var client = new StubWearableClient(WearableProvider.Fitbit, new WearableTokenResult(
            "access-token", "refresh-token", "ext-user-123", DateTime.UtcNow.AddHours(1)));
        var repo = new InMemoryWearableConnectionRepository();

        var handler = new ConnectWearableCommandHandler([client], repo);
        var result = await handler.Handle(
            new ConnectWearableCommand(userId.Value, "Fitbit", "auth-code"),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("Fitbit", result.Value.Provider);
        Assert.Equal("ext-user-123", result.Value.ExternalUserId);
        Assert.True(result.Value.IsActive);
    }

    [Fact]
    public async Task ConnectWearable_WithInvalidProvider_ReturnsFailure() {
        var handler = new ConnectWearableCommandHandler([], new InMemoryWearableConnectionRepository());

        var result = await handler.Handle(
            new ConnectWearableCommand(Guid.NewGuid(), "InvalidProvider", "code"),
            CancellationToken.None);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task ConnectWearable_WhenAuthFails_ReturnsFailure() {
        var client = new StubWearableClient(WearableProvider.Fitbit, null);
        var handler = new ConnectWearableCommandHandler([client], new InMemoryWearableConnectionRepository());

        var result = await handler.Handle(
            new ConnectWearableCommand(Guid.NewGuid(), "Fitbit", "bad-code"),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Contains("AuthFailed", result.Error.Code);
    }

    [Fact]
    public async Task ConnectWearable_WithExistingConnection_UpdatesTokens() {
        var userId = UserId.New();
        var existing = WearableConnection.Create(
            userId, WearableProvider.Fitbit, "ext-user", "old-token", "old-refresh", null);
        var repo = new InMemoryWearableConnectionRepository();
        repo.Seed(existing);

        var newToken = new WearableTokenResult("new-token", "new-refresh", "ext-user", DateTime.UtcNow.AddHours(1));
        var client = new StubWearableClient(WearableProvider.Fitbit, newToken);

        var handler = new ConnectWearableCommandHandler([client], repo);
        var result = await handler.Handle(
            new ConnectWearableCommand(userId.Value, "Fitbit", "code"),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task DisconnectWearable_WithActiveConnection_Succeeds() {
        var userId = UserId.New();
        var connection = WearableConnection.Create(
            userId, WearableProvider.Fitbit, "ext", "token", null, null);
        var repo = new InMemoryWearableConnectionRepository();
        repo.Seed(connection);

        var handler = new DisconnectWearableCommandHandler(repo);
        var result = await handler.Handle(
            new DisconnectWearableCommand(userId.Value, "Fitbit"),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.False(connection.IsActive);
    }

    [Fact]
    public async Task DisconnectWearable_WhenNotConnected_ReturnsFailure() {
        var handler = new DisconnectWearableCommandHandler(new InMemoryWearableConnectionRepository());

        var result = await handler.Handle(
            new DisconnectWearableCommand(Guid.NewGuid(), "Fitbit"),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Contains("NotConnected", result.Error.Code);
    }

    [Fact]
    public async Task DisconnectWearable_WithInvalidProvider_ReturnsFailure() {
        var handler = new DisconnectWearableCommandHandler(new InMemoryWearableConnectionRepository());

        var result = await handler.Handle(
            new DisconnectWearableCommand(Guid.NewGuid(), "Unknown"),
            CancellationToken.None);

        Assert.True(result.IsFailure);
    }

    private sealed class StubWearableClient(WearableProvider provider, WearableTokenResult? tokenResult) : IWearableClient {
        public WearableProvider Provider => provider;
        public string GetAuthorizationUrl(string state) => $"https://auth.example.com?state={state}";
        public Task<WearableTokenResult?> ExchangeCodeAsync(string code, CancellationToken ct = default) =>
            Task.FromResult(tokenResult);
        public Task<WearableTokenResult?> RefreshTokenAsync(string refreshToken, CancellationToken ct = default) =>
            throw new NotSupportedException();
        public Task<IReadOnlyList<WearableDataPoint>> FetchDailyDataAsync(string accessToken, DateTime date, CancellationToken ct = default) =>
            throw new NotSupportedException();
    }

    private sealed class InMemoryWearableConnectionRepository : IWearableConnectionRepository {
        private readonly List<WearableConnection> _connections = [];

        public void Seed(WearableConnection connection) => _connections.Add(connection);

        public Task<WearableConnection?> GetAsync(UserId userId, WearableProvider provider, CancellationToken ct = default) =>
            Task.FromResult(_connections.FirstOrDefault(c => c.UserId == userId && c.Provider == provider));

        public Task<IReadOnlyList<WearableConnection>> GetAllForUserAsync(UserId userId, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<WearableConnection>>(_connections.Where(c => c.UserId == userId).ToList());

        public Task<WearableConnection> AddAsync(WearableConnection connection, CancellationToken ct = default) {
            _connections.Add(connection);
            return Task.FromResult(connection);
        }

        public Task UpdateAsync(WearableConnection connection, CancellationToken ct = default) => Task.CompletedTask;
    }
}
