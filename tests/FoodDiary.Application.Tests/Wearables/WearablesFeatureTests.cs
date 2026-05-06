using FoodDiary.Application.Wearables.Commands.ConnectWearable;
using FoodDiary.Application.Wearables.Commands.DisconnectWearable;
using FoodDiary.Application.Wearables.Commands.SyncWearableData;
using FoodDiary.Application.Abstractions.Wearables.Common;
using FoodDiary.Application.Abstractions.Wearables.Models;
using FoodDiary.Application.Wearables.Queries.GetWearableAuthUrl;
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

    [Fact]
    public async Task GetWearableAuthUrl_WithConfiguredProvider_ReturnsClientUrl() {
        var client = new StubWearableClient(WearableProvider.Fitbit, null);
        var handler = new GetWearableAuthUrlQueryHandler([client]);

        var result = await handler.Handle(new GetWearableAuthUrlQuery("Fitbit", "state-123"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("https://auth.example.com?state=state-123", result.Value);
    }

    [Fact]
    public async Task GetWearableAuthUrl_WithUnconfiguredProvider_ReturnsFailure() {
        var handler = new GetWearableAuthUrlQueryHandler([]);

        var result = await handler.Handle(new GetWearableAuthUrlQuery("Fitbit", "state"), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Contains("ProviderNotConfigured", result.Error.Code);
    }

    [Fact]
    public async Task SyncWearableData_WithDailyData_AddsEntriesAndReturnsSummary() {
        var userId = UserId.New();
        var date = new DateTime(2026, 5, 6, 0, 0, 0, DateTimeKind.Utc);
        var connection = WearableConnection.Create(
            userId, WearableProvider.Fitbit, "ext", "access", "refresh", DateTime.UtcNow.AddHours(1));
        var connectionRepository = new InMemoryWearableConnectionRepository();
        connectionRepository.Seed(connection);
        var syncRepository = new InMemoryWearableSyncRepository();
        var client = new StubWearableClient(WearableProvider.Fitbit, null) {
            DataPoints = [
                new WearableDataPoint(WearableDataType.Steps, 5000),
                new WearableDataPoint(WearableDataType.CaloriesBurned, 250)
            ]
        };
        var handler = new SyncWearableDataCommandHandler([client], connectionRepository, syncRepository);

        var result = await handler.Handle(
            new SyncWearableDataCommand(userId.Value, "Fitbit", date),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(5000, result.Value.Steps);
        Assert.Equal(250, result.Value.CaloriesBurned);
        Assert.Equal(2, syncRepository.AddedCount);
        Assert.True(connectionRepository.UpdateCalled);
    }

    [Fact]
    public async Task SyncWearableData_WhenTokenRefreshFails_DeactivatesConnectionAndReturnsFailure() {
        var userId = UserId.New();
        var connection = WearableConnection.Create(
            userId, WearableProvider.Fitbit, "ext", "access", "refresh", DateTime.UtcNow.AddMinutes(-1));
        var connectionRepository = new InMemoryWearableConnectionRepository();
        connectionRepository.Seed(connection);
        var client = new StubWearableClient(WearableProvider.Fitbit, null) {
            RefreshTokenResult = null
        };
        var handler = new SyncWearableDataCommandHandler([client], connectionRepository, new InMemoryWearableSyncRepository());

        var result = await handler.Handle(
            new SyncWearableDataCommand(userId.Value, "Fitbit", DateTime.UtcNow.Date),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Contains("AuthFailed", result.Error.Code);
        Assert.False(connection.IsActive);
        Assert.True(connectionRepository.UpdateCalled);
    }

    private sealed class StubWearableClient(WearableProvider provider, WearableTokenResult? tokenResult) : IWearableClient {
        public WearableProvider Provider => provider;
        public WearableTokenResult? RefreshTokenResult { get; init; } = new("new-access", "new-refresh", "ext", DateTime.UtcNow.AddHours(1));
        public IReadOnlyList<WearableDataPoint> DataPoints { get; init; } = [];

        public string GetAuthorizationUrl(string state) => $"https://auth.example.com?state={state}";
        public Task<WearableTokenResult?> ExchangeCodeAsync(string code, CancellationToken ct = default) =>
            Task.FromResult(tokenResult);
        public Task<WearableTokenResult?> RefreshTokenAsync(string refreshToken, CancellationToken ct = default) =>
            Task.FromResult(RefreshTokenResult);
        public Task<IReadOnlyList<WearableDataPoint>> FetchDailyDataAsync(string accessToken, DateTime date, CancellationToken ct = default) =>
            Task.FromResult(DataPoints);
    }

    private sealed class InMemoryWearableConnectionRepository : IWearableConnectionRepository {
        private readonly List<WearableConnection> _connections = [];
        public bool UpdateCalled { get; private set; }

        public void Seed(WearableConnection connection) => _connections.Add(connection);

        public Task<WearableConnection?> GetAsync(UserId userId, WearableProvider provider, CancellationToken ct = default) =>
            Task.FromResult(_connections.FirstOrDefault(c => c.UserId == userId && c.Provider == provider));

        public Task<IReadOnlyList<WearableConnection>> GetAllForUserAsync(UserId userId, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<WearableConnection>>(_connections.Where(c => c.UserId == userId).ToList());

        public Task<WearableConnection> AddAsync(WearableConnection connection, CancellationToken ct = default) {
            _connections.Add(connection);
            return Task.FromResult(connection);
        }

        public Task UpdateAsync(WearableConnection connection, CancellationToken ct = default) {
            UpdateCalled = true;
            return Task.CompletedTask;
        }
    }

    private sealed class InMemoryWearableSyncRepository : IWearableSyncRepository {
        private readonly List<WearableSyncEntry> _entries = [];
        public int AddedCount { get; private set; }

        public Task<WearableSyncEntry?> GetAsync(
            UserId userId,
            WearableProvider provider,
            WearableDataType dataType,
            DateTime date,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(_entries.FirstOrDefault(e =>
                e.UserId == userId &&
                e.Provider == provider &&
                e.DataType == dataType &&
                e.Date == date.Date));

        public Task<IReadOnlyList<WearableSyncEntry>> GetDailySummaryAsync(
            UserId userId,
            DateTime date,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<WearableSyncEntry>>(
                _entries.Where(e => e.UserId == userId && e.Date == date.Date).ToList());

        public Task<WearableSyncEntry> AddAsync(WearableSyncEntry entry, CancellationToken cancellationToken = default) {
            AddedCount++;
            _entries.Add(entry);
            return Task.FromResult(entry);
        }

        public Task UpdateAsync(WearableSyncEntry entry, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }
}
