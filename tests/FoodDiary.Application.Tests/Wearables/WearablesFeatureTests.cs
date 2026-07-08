using FoodDiary.Application.Wearables.Commands.ConnectWearable;
using FoodDiary.Application.Wearables.Commands.DisconnectWearable;
using FoodDiary.Application.Wearables.Commands.SyncWearableData;
using FoodDiary.Application.Abstractions.Wearables.Common;
using FoodDiary.Application.Abstractions.Wearables.Models;
using FoodDiary.Application.Wearables.Common;
using FoodDiary.Application.Wearables.Queries.GetWearableAuthUrl;
using FoodDiary.Application.Wearables.Queries.GetWearableConnections;
using FoodDiary.Application.Wearables.Queries.GetWearableDailySummary;
using FoodDiary.Application.Wearables.Services;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Domain.Entities.Wearables;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Results;

namespace FoodDiary.Application.Tests.Wearables;

[ExcludeFromCodeCoverage]
public class WearablesFeatureTests {
    [Fact]
    public async Task ConnectWearable_WithValidCode_CreatesConnection() {
        var userId = UserId.New();
        var client = new StubWearableClient(WearableProvider.Fitbit, new WearableTokenResult(
            "access-token", "refresh-token", "ext-user-123", DateTime.UtcNow.AddHours(1)));
        var repo = new InMemoryWearableConnectionRepository();

        var stateService = new StubWearableOAuthStateService();
        string state = stateService.CreateState(userId, WearableProvider.Fitbit, "state-123");
        var handler = new ConnectWearableCommandHandler([client], repo, stateService, CreateCurrentUserAccessService());
        Result<WearableConnectionModel> result = await handler.Handle(
            new ConnectWearableCommand(userId.Value, "Fitbit", "auth-code", state),
            CancellationToken.None);

        ResultAssert.Success(result);
        Assert.Equal("Fitbit", result.Value.Provider);
        Assert.Equal("ext-user-123", result.Value.ExternalUserId);
        Assert.True(result.Value.IsActive);
    }

    [Fact]
    public async Task ConnectWearable_WithInvalidProvider_ReturnsFailure() {
        var handler = new ConnectWearableCommandHandler(
            [],
            new InMemoryWearableConnectionRepository(),
            new StubWearableOAuthStateService(),
            CreateCurrentUserAccessService());

        Result<WearableConnectionModel> result = await handler.Handle(
            new ConnectWearableCommand(Guid.NewGuid(), "InvalidProvider", "code", "state"),
            CancellationToken.None);

        ResultAssert.Failure(result);
    }

    [Fact]
    public async Task ConnectWearable_WithEmptyUserId_ReturnsInvalidToken() {
        var handler = new ConnectWearableCommandHandler(
            [],
            new InMemoryWearableConnectionRepository(),
            new StubWearableOAuthStateService(),
            CreateCurrentUserAccessService());

        Result<WearableConnectionModel> result = await handler.Handle(
            new ConnectWearableCommand(Guid.Empty, "Fitbit", "code", "state"),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Authentication.InvalidToken", result.Error.Code);
    }

    [Fact]
    public async Task ConnectWearable_WithUnconfiguredProvider_ReturnsProviderNotConfigured() {
        var handler = new ConnectWearableCommandHandler(
            [],
            new InMemoryWearableConnectionRepository(),
            new StubWearableOAuthStateService(),
            CreateCurrentUserAccessService());

        Result<WearableConnectionModel> result = await handler.Handle(
            new ConnectWearableCommand(Guid.NewGuid(), "Fitbit", "code", "state"),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Wearable.ProviderNotConfigured", result.Error.Code);
    }

    [Fact]
    public async Task ConnectWearable_WhenAuthFails_ReturnsFailure() {
        var client = new StubWearableClient(WearableProvider.Fitbit, tokenResult: null);
        var userId = UserId.New();
        var stateService = new StubWearableOAuthStateService();
        string state = stateService.CreateState(userId, WearableProvider.Fitbit, "state-123");
        var handler = new ConnectWearableCommandHandler([client], new InMemoryWearableConnectionRepository(), stateService, CreateCurrentUserAccessService());

        Result<WearableConnectionModel> result = await handler.Handle(
            new ConnectWearableCommand(userId.Value, "Fitbit", "bad-code", state),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Contains("AuthFailed", result.Error.Code, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ConnectWearable_WithExistingConnection_UpdatesTokens() {
        var userId = UserId.New();
        var existing = WearableConnection.Create(
            userId, WearableProvider.Fitbit, "ext-user", "old-token", "old-refresh", tokenExpiresAtUtc: null);
        var repo = new InMemoryWearableConnectionRepository();
        repo.Seed(existing);

        var newToken = new WearableTokenResult("new-token", "new-refresh", "ext-user", DateTime.UtcNow.AddHours(1));
        var client = new StubWearableClient(WearableProvider.Fitbit, newToken);

        var stateService = new StubWearableOAuthStateService();
        string state = stateService.CreateState(userId, WearableProvider.Fitbit, "state-123");
        var handler = new ConnectWearableCommandHandler([client], repo, stateService, CreateCurrentUserAccessService());
        Result<WearableConnectionModel> result = await handler.Handle(
            new ConnectWearableCommand(userId.Value, "Fitbit", "code", state),
            CancellationToken.None);

        ResultAssert.Success(result);
    }

    [Fact]
    public async Task ConnectWearable_WithInactiveExistingConnection_ReplacesConnectionInResult() {
        var userId = UserId.New();
        var existing = WearableConnection.Create(
            userId, WearableProvider.Fitbit, "old-ext-user", "old-token", "old-refresh", tokenExpiresAtUtc: null);
        existing.Deactivate();
        var repo = new InMemoryWearableConnectionRepository();
        repo.Seed(existing);

        var token = new WearableTokenResult("new-token", "new-refresh", "new-ext-user", DateTime.UtcNow.AddHours(1));
        var client = new StubWearableClient(WearableProvider.Fitbit, token);

        var stateService = new StubWearableOAuthStateService();
        string state = stateService.CreateState(userId, WearableProvider.Fitbit, "state-123");
        var handler = new ConnectWearableCommandHandler([client], repo, stateService, CreateCurrentUserAccessService());
        Result<WearableConnectionModel> result = await handler.Handle(
            new ConnectWearableCommand(userId.Value, "Fitbit", "code", state),
            CancellationToken.None);

        ResultAssert.Success(result);
        Assert.True(result.Value.IsActive);
        Assert.Equal("new-ext-user", result.Value.ExternalUserId);
        Assert.True(repo.UpdateCalled);
    }

    [Fact]
    public async Task ConnectWearable_WithInvalidState_ReturnsFailureBeforeTokenExchange() {
        var userId = UserId.New();
        var client = new StubWearableClient(WearableProvider.Fitbit, new WearableTokenResult(
            "access-token", "refresh-token", "ext-user-123", DateTime.UtcNow.AddHours(1)));
        var handler = new ConnectWearableCommandHandler(
            [client],
            new InMemoryWearableConnectionRepository(),
            new StubWearableOAuthStateService(),
            CreateCurrentUserAccessService());

        Result<WearableConnectionModel> result = await handler.Handle(
            new ConnectWearableCommand(userId.Value, "Fitbit", "auth-code", "tampered-state"),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Contains("InvalidState", result.Error.Code, StringComparison.Ordinal);
        Assert.Equal(0, client.ExchangeCodeCallCount);
    }

    [Fact]
    public async Task DisconnectWearable_WithActiveConnection_Succeeds() {
        var userId = UserId.New();
        var connection = WearableConnection.Create(
            userId, WearableProvider.Fitbit, "ext", "token", refreshToken: null, tokenExpiresAtUtc: null);
        var repo = new InMemoryWearableConnectionRepository();
        repo.Seed(connection);

        var handler = new DisconnectWearableCommandHandler(repo, CreateCurrentUserAccessService());
        Result result = await handler.Handle(
            new DisconnectWearableCommand(userId.Value, "Fitbit"),
            CancellationToken.None);

        ResultAssert.Success(result);
        Assert.False(connection.IsActive);
    }

    [Fact]
    public async Task DisconnectWearable_WhenNotConnected_ReturnsFailure() {
        var handler = new DisconnectWearableCommandHandler(new InMemoryWearableConnectionRepository(), CreateCurrentUserAccessService());

        Result result = await handler.Handle(
            new DisconnectWearableCommand(Guid.NewGuid(), "Fitbit"),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Contains("NotConnected", result.Error.Code, StringComparison.Ordinal);
    }

    [Fact]
    public async Task DisconnectWearable_WithInvalidProvider_ReturnsFailure() {
        var handler = new DisconnectWearableCommandHandler(new InMemoryWearableConnectionRepository(), CreateCurrentUserAccessService());

        Result result = await handler.Handle(
            new DisconnectWearableCommand(Guid.NewGuid(), "Unknown"),
            CancellationToken.None);

        ResultAssert.Failure(result);
    }

    [Fact]
    public async Task DisconnectWearable_WithEmptyUserId_ReturnsInvalidToken() {
        var handler = new DisconnectWearableCommandHandler(new InMemoryWearableConnectionRepository(), CreateCurrentUserAccessService());

        Result result = await handler.Handle(
            new DisconnectWearableCommand(Guid.Empty, "Fitbit"),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Authentication.InvalidToken", result.Error.Code);
    }

    [Fact]
    public async Task GetWearableAuthUrl_WithConfiguredProvider_ReturnsClientUrl() {
        var client = new StubWearableClient(WearableProvider.Fitbit, tokenResult: null);
        var userId = UserId.New();
        var handler = new GetWearableAuthUrlQueryHandler([client], new StubWearableOAuthStateService(), CreateCurrentUserAccessService());

        Result<string> result = await handler.Handle(
            new GetWearableAuthUrlQuery(userId.Value, "Fitbit", "state-123"),
            CancellationToken.None);

        ResultAssert.Success(result);
        Assert.Equal("https://auth.example.com?state=Fitbit:state-123", result.Value);
    }

    [Fact]
    public async Task GetWearableAuthUrl_WithUnconfiguredProvider_ReturnsFailure() {
        var handler = new GetWearableAuthUrlQueryHandler([], new StubWearableOAuthStateService(), CreateCurrentUserAccessService());

        Result<string> result = await handler.Handle(new GetWearableAuthUrlQuery(Guid.NewGuid(), "Fitbit", "state"), CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Contains("ProviderNotConfigured", result.Error.Code, StringComparison.Ordinal);
    }

    [Fact]
    public async Task GetWearableAuthUrl_WithEmptyUserId_ReturnsInvalidToken() {
        var handler = new GetWearableAuthUrlQueryHandler(
            [new StubWearableClient(WearableProvider.Fitbit, tokenResult: null)],
            new StubWearableOAuthStateService(),
            CreateCurrentUserAccessService());

        Result<string> result = await handler.Handle(new GetWearableAuthUrlQuery(Guid.Empty, "Fitbit", "state"), CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Authentication.InvalidToken", result.Error.Code);
    }

    [Fact]
    public async Task GetWearableAuthUrl_WithInvalidProvider_ReturnsInvalidProvider() {
        var handler = new GetWearableAuthUrlQueryHandler(
            [new StubWearableClient(WearableProvider.Fitbit, tokenResult: null)],
            new StubWearableOAuthStateService(),
            CreateCurrentUserAccessService());

        Result<string> result = await handler.Handle(new GetWearableAuthUrlQuery(Guid.NewGuid(), "Unknown", "state"), CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Wearable.InvalidProvider", result.Error.Code);
    }

    [Fact]
    public async Task GetWearableDailySummary_WithEmptyUserId_ReturnsInvalidToken() {
        var handler = new GetWearableDailySummaryQueryHandler(CreateWearableReadService(), CreateCurrentUserAccessService());

        Result<WearableDailySummaryModel> result = await handler.Handle(
            new GetWearableDailySummaryQuery(Guid.Empty, DateTime.UtcNow.Date),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Authentication.InvalidToken", result.Error.Code);
    }

    [Fact]
    public async Task GetWearableDailySummary_WithEntries_ReturnsMappedSummary() {
        var userId = UserId.New();
        var date = new DateTime(2026, 5, 8, 0, 0, 0, DateTimeKind.Utc);
        var repository = new InMemoryWearableSyncRepository();
        repository.Seed(WearableSyncEntry.Create(userId, WearableProvider.Fitbit, WearableDataType.Steps, date, 1000));
        repository.Seed(WearableSyncEntry.Create(userId, WearableProvider.Fitbit, WearableDataType.CaloriesBurned, date, 75));
        var handler = new GetWearableDailySummaryQueryHandler(CreateWearableReadService(syncRepository: repository), CreateCurrentUserAccessService());

        Result<WearableDailySummaryModel> result = await handler.Handle(new GetWearableDailySummaryQuery(userId.Value, date), CancellationToken.None);

        ResultAssert.Success(result);
        Assert.Equal(date.Date, result.Value.Date);
        Assert.Equal(1000, result.Value.Steps);
        Assert.Equal(75, result.Value.CaloriesBurned);
    }

    [Fact]
    public async Task GetWearableConnections_WithEmptyUserId_ReturnsInvalidToken() {
        var handler = new GetWearableConnectionsQueryHandler(CreateWearableReadService(), CreateCurrentUserAccessService());

        Result<IReadOnlyList<WearableConnectionModel>> result = await handler.Handle(new GetWearableConnectionsQuery(Guid.Empty), CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Authentication.InvalidToken", result.Error.Code);
    }

    [Fact]
    public async Task GetWearableConnections_WithConnections_ReturnsModels() {
        var userId = UserId.New();
        var connection = WearableConnection.Create(
            userId,
            WearableProvider.Fitbit,
            "external",
            "access",
            "refresh",
            DateTime.UtcNow.AddHours(1));
        var repository = new InMemoryWearableConnectionRepository();
        repository.Seed(connection);
        var handler = new GetWearableConnectionsQueryHandler(CreateWearableReadService(connectionRepository: repository), CreateCurrentUserAccessService());

        Result<IReadOnlyList<WearableConnectionModel>> result = await handler.Handle(new GetWearableConnectionsQuery(userId.Value), CancellationToken.None);

        ResultAssert.Success(result);
        WearableConnectionModel model = Assert.Single(result.Value);
        Assert.Equal("Fitbit", model.Provider);
        Assert.Equal("external", model.ExternalUserId);
        Assert.True(model.IsActive);
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
        var client = new StubWearableClient(WearableProvider.Fitbit, tokenResult: null) {
            DataPoints = [
                new WearableDataPoint(WearableDataType.Steps, 5000),
                new WearableDataPoint(WearableDataType.CaloriesBurned, 250),
            ],
        };
        var handler = new SyncWearableDataCommandHandler([client], connectionRepository, syncRepository, CreateCurrentUserAccessService());

        Result<WearableDailySummaryModel> result = await handler.Handle(
            new SyncWearableDataCommand(userId.Value, "Fitbit", date),
            CancellationToken.None);

        ResultAssert.Success(result);
        Assert.Equal(5000, result.Value.Steps);
        Assert.Equal(250, result.Value.CaloriesBurned);
        Assert.Equal(2, syncRepository.AddedCount);
        Assert.True(connectionRepository.UpdateCalled);
    }

    [Fact]
    public async Task SyncWearableData_WithInvalidUserId_ReturnsInvalidToken() {
        var handler = new SyncWearableDataCommandHandler(
            [],
            new InMemoryWearableConnectionRepository(),
            new InMemoryWearableSyncRepository(),
            CreateCurrentUserAccessService());

        Result<WearableDailySummaryModel> result = await handler.Handle(
            new SyncWearableDataCommand(Guid.Empty, "Fitbit", DateTime.UtcNow.Date),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Authentication.InvalidToken", result.Error.Code);
    }

    [Fact]
    public async Task SyncWearableData_WithInvalidProvider_ReturnsInvalidProvider() {
        var handler = new SyncWearableDataCommandHandler(
            [],
            new InMemoryWearableConnectionRepository(),
            new InMemoryWearableSyncRepository(),
            CreateCurrentUserAccessService());

        Result<WearableDailySummaryModel> result = await handler.Handle(
            new SyncWearableDataCommand(Guid.NewGuid(), "Unknown", DateTime.UtcNow.Date),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Wearable.InvalidProvider", result.Error.Code);
    }

    [Fact]
    public async Task SyncWearableData_WhenConnectionMissing_ReturnsNotConnected() {
        var handler = new SyncWearableDataCommandHandler(
            [new StubWearableClient(WearableProvider.Fitbit, tokenResult: null)],
            new InMemoryWearableConnectionRepository(),
            new InMemoryWearableSyncRepository(),
            CreateCurrentUserAccessService());

        Result<WearableDailySummaryModel> result = await handler.Handle(
            new SyncWearableDataCommand(Guid.NewGuid(), "Fitbit", DateTime.UtcNow.Date),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Wearable.NotConnected", result.Error.Code);
    }

    [Fact]
    public async Task SyncWearableData_WhenConnectionInactive_ReturnsNotConnected() {
        var userId = UserId.New();
        var connection = WearableConnection.Create(
            userId, WearableProvider.Fitbit, "ext", "access", "refresh", DateTime.UtcNow.AddHours(1));
        connection.Deactivate();
        var connectionRepository = new InMemoryWearableConnectionRepository();
        connectionRepository.Seed(connection);
        var handler = new SyncWearableDataCommandHandler(
            [new StubWearableClient(WearableProvider.Fitbit, tokenResult: null)],
            connectionRepository,
            new InMemoryWearableSyncRepository(),
            CreateCurrentUserAccessService());

        Result<WearableDailySummaryModel> result = await handler.Handle(
            new SyncWearableDataCommand(userId.Value, "Fitbit", DateTime.UtcNow.Date),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Wearable.NotConnected", result.Error.Code);
    }

    [Fact]
    public async Task SyncWearableData_WhenProviderClientMissing_ReturnsProviderNotConfigured() {
        var userId = UserId.New();
        var connection = WearableConnection.Create(
            userId, WearableProvider.Fitbit, "ext", "access", "refresh", DateTime.UtcNow.AddHours(1));
        var connectionRepository = new InMemoryWearableConnectionRepository();
        connectionRepository.Seed(connection);
        var handler = new SyncWearableDataCommandHandler(
            [],
            connectionRepository,
            new InMemoryWearableSyncRepository(),
            CreateCurrentUserAccessService());

        Result<WearableDailySummaryModel> result = await handler.Handle(
            new SyncWearableDataCommand(userId.Value, "Fitbit", DateTime.UtcNow.Date),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Wearable.ProviderNotConfigured", result.Error.Code);
    }

    [Fact]
    public async Task SyncWearableData_WhenTokenExpired_RefreshesConnectionTokens() {
        var userId = UserId.New();
        var connection = WearableConnection.Create(
            userId, WearableProvider.Fitbit, "ext", "old-access", "old-refresh", DateTime.UtcNow.AddMinutes(-1));
        var connectionRepository = new InMemoryWearableConnectionRepository();
        connectionRepository.Seed(connection);
        DateTime refreshExpiresAtUtc = DateTime.UtcNow.AddHours(2);
        var client = new StubWearableClient(WearableProvider.Fitbit, tokenResult: null) {
            RefreshTokenResult = new WearableTokenResult("new-access", "new-refresh", "ext", refreshExpiresAtUtc),
        };
        var handler = new SyncWearableDataCommandHandler([client], connectionRepository, new InMemoryWearableSyncRepository(), CreateCurrentUserAccessService());

        Result<WearableDailySummaryModel> result = await handler.Handle(
            new SyncWearableDataCommand(userId.Value, "Fitbit", DateTime.UtcNow.Date),
            CancellationToken.None);

        ResultAssert.Success(result);
        Assert.Equal("new-access", connection.AccessToken);
        Assert.Equal("new-refresh", connection.RefreshToken);
        Assert.Equal(2, connectionRepository.UpdateCallCount);
    }

    [Fact]
    public async Task SyncWearableData_WithExistingEntry_UpdatesValueAndBuildsFullSummary() {
        var userId = UserId.New();
        var date = new DateTime(2026, 5, 7, 0, 0, 0, DateTimeKind.Utc);
        var connection = WearableConnection.Create(
            userId, WearableProvider.Fitbit, "ext", "access", "refresh", DateTime.UtcNow.AddHours(1));
        var connectionRepository = new InMemoryWearableConnectionRepository();
        connectionRepository.Seed(connection);
        var syncRepository = new InMemoryWearableSyncRepository();
        syncRepository.Seed(WearableSyncEntry.Create(userId, WearableProvider.Fitbit, WearableDataType.ActiveMinutes, date, 10));
        var client = new StubWearableClient(WearableProvider.Fitbit, tokenResult: null) {
            DataPoints = [
                new WearableDataPoint(WearableDataType.HeartRate, 72),
                new WearableDataPoint(WearableDataType.ActiveMinutes, 20),
                new WearableDataPoint(WearableDataType.SleepMinutes, 420),
            ],
        };
        var handler = new SyncWearableDataCommandHandler([client], connectionRepository, syncRepository, CreateCurrentUserAccessService());

        Result<WearableDailySummaryModel> result = await handler.Handle(
            new SyncWearableDataCommand(userId.Value, "Fitbit", date),
            CancellationToken.None);

        ResultAssert.Success(result);
        Assert.Equal(72, result.Value.HeartRate);
        Assert.Equal(20, result.Value.ActiveMinutes);
        Assert.Equal(420, result.Value.SleepMinutes);
        Assert.Equal(1, syncRepository.UpdatedCount);
        Assert.Equal(2, syncRepository.AddedCount);
    }

    [Fact]
    public async Task SyncWearableData_WhenTokenRefreshFails_DeactivatesConnectionAndReturnsFailure() {
        var userId = UserId.New();
        var connection = WearableConnection.Create(
            userId, WearableProvider.Fitbit, "ext", "access", "refresh", DateTime.UtcNow.AddMinutes(-1));
        var connectionRepository = new InMemoryWearableConnectionRepository();
        connectionRepository.Seed(connection);
        var client = new StubWearableClient(WearableProvider.Fitbit, tokenResult: null) {
            RefreshTokenResult = null,
        };
        var handler = new SyncWearableDataCommandHandler([client], connectionRepository, new InMemoryWearableSyncRepository(), CreateCurrentUserAccessService());

        Result<WearableDailySummaryModel> result = await handler.Handle(
            new SyncWearableDataCommand(userId.Value, "Fitbit", DateTime.UtcNow.Date),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Contains("AuthFailed", result.Error.Code, StringComparison.Ordinal);
        Assert.False(connection.IsActive);
        Assert.True(connectionRepository.UpdateCalled);
    }

    private static IWearableReadService CreateWearableReadService(
        IWearableConnectionReadRepository? connectionRepository = null,
        IWearableSyncReadModelRepository? syncRepository = null) =>
        new WearableReadService(
            connectionRepository ?? new InMemoryWearableConnectionRepository(),
            syncRepository ?? new InMemoryWearableSyncRepository());

    private static ICurrentUserAccessService CreateCurrentUserAccessService(Error? accessError = null) {
        ICurrentUserAccessService service = Substitute.For<ICurrentUserAccessService>();
        service
            .EnsureCanAccessAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(accessError));
        return service;
    }

    [ExcludeFromCodeCoverage]
    private sealed class StubWearableClient(WearableProvider provider, WearableTokenResult? tokenResult) : IWearableClient {
        public WearableProvider Provider => provider;
        public WearableTokenResult? RefreshTokenResult { get; init; } = new("new-access", "new-refresh", "ext", DateTime.UtcNow.AddHours(1));
        public IReadOnlyList<WearableDataPoint> DataPoints { get; init; } = [];
        public int ExchangeCodeCallCount { get; private set; }

        public string GetAuthorizationUrl(string state) => $"https://auth.example.com?state={state}";
        public Task<WearableTokenResult?> ExchangeCodeAsync(string code, CancellationToken ct = default) {
            ExchangeCodeCallCount++;
            return Task.FromResult(tokenResult);
        }
        public Task<WearableTokenResult?> RefreshTokenAsync(string refreshToken, CancellationToken ct = default) =>
            Task.FromResult(RefreshTokenResult);
        public Task<IReadOnlyList<WearableDataPoint>> FetchDailyDataAsync(string accessToken, DateTime date, CancellationToken ct = default) =>
            Task.FromResult(DataPoints);
    }

    [ExcludeFromCodeCoverage]
    private sealed class StubWearableOAuthStateService : IWearableOAuthStateService {
        private readonly HashSet<string> _validStates = [];

        public string CreateState(UserId userId, WearableProvider provider, string? clientState) {
            string state = $"{provider}:{clientState}";
            _validStates.Add(state);
            return state;
        }

        public bool IsValidState(string state, UserId userId, WearableProvider provider) =>
            _validStates.Contains(state) && state.StartsWith($"{provider}:", StringComparison.Ordinal);
    }

    [ExcludeFromCodeCoverage]
    private sealed class InMemoryWearableConnectionRepository : IWearableConnectionRepository {
        private readonly List<WearableConnection> _connections = [];
        public bool UpdateCalled { get; private set; }
        public int UpdateCallCount { get; private set; }

        public void Seed(WearableConnection connection) => _connections.Add(connection);

        public Task<WearableConnection?> GetAsync(UserId userId, WearableProvider provider, CancellationToken ct = default) =>
            Task.FromResult(_connections.FirstOrDefault(c => c.UserId == userId && c.Provider == provider));

        public Task<IReadOnlyList<WearableConnection>> GetAllForUserAsync(UserId userId, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<WearableConnection>>(_connections.Where(c => c.UserId == userId).ToList());

        public Task<IReadOnlyList<WearableConnectionModel>> GetConnectionModelsAsync(UserId userId, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<WearableConnectionModel>>([.. _connections
                .Where(connection => connection.UserId == userId)
                .Select(connection => new WearableConnectionModel(
                    connection.Provider.ToString(),
                    connection.ExternalUserId,
                    connection.IsActive,
                    connection.LastSyncedAtUtc,
                    connection.CreatedOnUtc))]);

        public Task<WearableConnection> AddAsync(WearableConnection connection, CancellationToken ct = default) {
            _connections.Add(connection);
            return Task.FromResult(connection);
        }

        public Task UpdateAsync(WearableConnection connection, CancellationToken ct = default) {
            UpdateCalled = true;
            UpdateCallCount++;
            return Task.CompletedTask;
        }
    }

    [ExcludeFromCodeCoverage]
    private sealed class InMemoryWearableSyncRepository : IWearableSyncRepository {
        private readonly List<WearableSyncEntry> _entries = [];
        public int AddedCount { get; private set; }
        public int UpdatedCount { get; private set; }

        public void Seed(WearableSyncEntry entry) => _entries.Add(entry);

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

        public Task<IReadOnlyList<WearableSyncEntryReadModel>> GetDailySummaryReadModelsAsync(
            UserId userId,
            DateTime date,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<WearableSyncEntryReadModel>>(
                [.. _entries
                    .Where(entry => entry.UserId == userId && entry.Date == date.Date)
                    .Select(entry => new WearableSyncEntryReadModel(entry.DataType, entry.Value))]);

        public Task<WearableSyncEntry> AddAsync(WearableSyncEntry entry, CancellationToken cancellationToken = default) {
            AddedCount++;
            _entries.Add(entry);
            return Task.FromResult(entry);
        }

        public Task UpdateAsync(WearableSyncEntry entry, CancellationToken cancellationToken = default) {
            UpdatedCount++;
            return Task.CompletedTask;
        }
    }
}
