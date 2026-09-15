using FoodDiary.Application.Abstractions.Common.Abstractions.Persistence;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.Wearables.Application.Abstractions.Common;
using FoodDiary.Modules.Wearables.Application.Abstractions.Models;
using FoodDiary.Modules.Wearables.Application.Commands.ConnectWearable;
using FoodDiary.Modules.Wearables.Application.Commands.DisconnectWearable;
using FoodDiary.Modules.Wearables.Application.Commands.SyncWearableData;
using FoodDiary.Modules.Wearables.Domain.Entities;
using FoodDiary.Modules.Wearables.Domain.Enums;
using FoodDiary.Results;

namespace FoodDiary.Modules.Wearables.Application.Tests;

public sealed partial class WearablesFeatureTests {
    [Fact]
    public async Task SyncWearableData_WhenRefreshTemporarilyFails_PreservesConnectionAndTokens() {
        var userId = UserId.New();
        var connection = WearableConnection.Create(userId, WearableProvider.Fitbit, "ext",
            StoredToken("access"), StoredToken("refresh"), DateTime.UtcNow.AddMinutes(-1));
        var repository = new InMemoryWearableConnectionRepository();
        repository.Seed(connection);
        var client = new StubWearableClient(WearableProvider.Fitbit, tokenResult: null) {
            RefreshError = WearableErrors.SyncFailed("Fitbit"),
        };
        IUnitOfWork unitOfWork = CreateUnitOfWork();
        var handler = new SyncWearableDataCommandHandler([client], repository, new InMemoryWearableSyncRepository(),
            new SerializedWearableTransactionRunner(), CreateCurrentUserAccessService(), CreateTokenProtector(), unitOfWork);

        Result<WearableDailySummaryModel> result = await handler.Handle(
            new SyncWearableDataCommand(userId.Value, "Fitbit", DateTime.UtcNow.Date), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Wearable.SyncFailed", result.Error.Code);
        Assert.True(connection.IsActive);
        Assert.Equal("fdp1:access", connection.AccessToken.Value);
        Assert.Equal("fdp1:refresh", connection.RefreshToken?.Value);
        Assert.False(repository.UpdateCalled);
        await unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SyncWearableData_ForDifferentDays_RefreshesSharedConnectionOnce() {
        var userId = UserId.New();
        var connection = WearableConnection.Create(userId, WearableProvider.Fitbit, "ext",
            StoredToken("access"), StoredToken("refresh"), DateTime.UtcNow.AddMinutes(-1));
        var repository = new InMemoryWearableConnectionRepository();
        repository.Seed(connection);
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var client = new StubWearableClient(WearableProvider.Fitbit, tokenResult: null) {
            BeforeRefresh = async token => {
                entered.TrySetResult();
                await release.Task.WaitAsync(token);
            },
        };
        var runner = new SerializedWearableTransactionRunner();
        var handler = new SyncWearableDataCommandHandler([client], repository, new InMemoryWearableSyncRepository(),
            runner, CreateCurrentUserAccessService(), CreateTokenProtector(), CreateUnitOfWork());
        Task<Result<WearableDailySummaryModel>> first = handler.Handle(
            new SyncWearableDataCommand(userId.Value, "Fitbit", DateTime.UtcNow.Date.AddDays(-1)), CancellationToken.None);
        await entered.Task.WaitAsync(TimeSpan.FromSeconds(5));
        Task<Result<WearableDailySummaryModel>> second = handler.Handle(
            new SyncWearableDataCommand(userId.Value, "Fitbit", DateTime.UtcNow.Date), CancellationToken.None);
        release.SetResult();

        Result<WearableDailySummaryModel>[] results = await Task.WhenAll(first, second).WaitAsync(TimeSpan.FromSeconds(5));

        Assert.All(results, result => Assert.True(result.IsSuccess));
        Assert.Equal(1, client.RefreshCallCount);
        Assert.Equal(1, runner.MaxConcurrentOperations);
    }

    [Fact]
    public async Task DisconnectWearable_DuringConnect_WaitsThenDeactivatesCreatedConnection() {
        var userId = UserId.New();
        var repository = new InMemoryWearableConnectionRepository();
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var client = new StubWearableClient(WearableProvider.Fitbit,
            new WearableTokenResult("access", "refresh", "ext", DateTime.UtcNow.AddHours(1))) {
            BeforeExchange = async token => {
                entered.TrySetResult();
                await release.Task.WaitAsync(token);
            },
        };
        var runner = new SerializedWearableTransactionRunner();
        var states = new StubWearableOAuthStateService();
        string state = states.CreateState(userId, WearableProvider.Fitbit, "connect");
        var connect = new ConnectWearableCommandHandler([client], repository, runner, states,
            CreateCurrentUserAccessService(), CreateTokenProtector());
        var disconnect = new DisconnectWearableCommandHandler(repository, CreateCurrentUserAccessService(), runner);
        Task<Result<WearableConnectionModel>> connecting = connect.Handle(
            new ConnectWearableCommand(userId.Value, "Fitbit", "code", state, RequestId, RequestHash), CancellationToken.None);
        await entered.Task.WaitAsync(TimeSpan.FromSeconds(5));
        Task<Result> disconnecting = disconnect.Handle(new DisconnectWearableCommand(userId.Value, "Fitbit"), CancellationToken.None);
        release.SetResult();

        Assert.True((await connecting.WaitAsync(TimeSpan.FromSeconds(5))).IsSuccess);
        Assert.True((await disconnecting.WaitAsync(TimeSpan.FromSeconds(5))).IsSuccess);
        Assert.False(Assert.Single(repository.Connections).IsActive);
        Assert.Equal(1, runner.MaxConcurrentOperations);
    }
}
