using FoodDiary.Application.Abstractions.Common.Abstractions.Persistence;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Application.Abstractions.Wearables.Common;
using FoodDiary.Application.Abstractions.Wearables.Models;
using FoodDiary.Application.Wearables.Commands.SyncWearableData;
using FoodDiary.Domain.Entities.Wearables;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Infrastructure.Persistence;
using FoodDiary.Infrastructure.Persistence.Wearables;
using FoodDiary.Results;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Infrastructure.Tests;

[ExcludeFromCodeCoverage]
public sealed class LegacyTokenUpgradeTests {
    [Fact]
    public async Task SyncWearableData_WithLegacyAccessAndNoRefreshToken_UpgradesAccessToken() {
        DbContextOptions<FoodDiaryDbContext> options = new DbContextOptionsBuilder<FoodDiaryDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        await using var context = new FoodDiaryDbContext(options);
        var userId = UserId.New();
        var connection = WearableConnection.Create(
            userId,
            WearableProvider.Fitbit,
            "external-user",
            ProtectedWearableToken.FromProtectedValue("fdp1:initial"),
            refreshToken: null,
            tokenExpiresAtUtc: null);
        context.WearableConnections.Add(connection);
        await context.SaveChangesAsync();
        context.Entry(connection).Property(nameof(WearableConnection.AccessToken)).CurrentValue =
            ProtectedWearableToken.FromStoredValue("legacy-access-token");
        var connectionRepository = new WearableConnectionRepository(context);
        var syncRepository = new WearableSyncRepository(context);
        var protector = new TestTokenProtector();
        var handler = new SyncWearableDataCommandHandler(
            [new EmptyWearableClient()],
            connectionRepository,
            syncRepository,
            new InlineTransactionRunner(),
            new AllowCurrentUserAccessService(),
            protector,
            new TestUnitOfWork(context));

        Result<WearableDailySummaryModel> result = await handler.Handle(
            new SyncWearableDataCommand(userId.Value, "Fitbit", new DateTime(2026, 8, 15, 0, 0, 0, DateTimeKind.Utc)),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("fdp1:legacy-access-token", connection.AccessToken.Value);
        Assert.Null(connection.RefreshToken);
    }

    [ExcludeFromCodeCoverage]
    private sealed class InlineTransactionRunner : IWearableTransactionRunner {
        public Task<TResult> ExecuteSerializedAsync<TResult>(string serializationKey, Func<CancellationToken, Task<TResult>> operation, CancellationToken cancellationToken = default) =>
            operation(cancellationToken);
    }

    [ExcludeFromCodeCoverage]
    private sealed class TestUnitOfWork(FoodDiaryDbContext context) : IUnitOfWork {
        public bool HasPendingChanges => context.ChangeTracker.HasChanges();

        public async Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
            await context.SaveChangesAsync(cancellationToken);
    }

    [ExcludeFromCodeCoverage]
    private sealed class AllowCurrentUserAccessService : ICurrentUserAccessService {
        public Task<Error?> EnsureCanAccessAsync(UserId userId, CancellationToken cancellationToken = default) =>
            Task.FromResult<Error?>(null);
    }

    [ExcludeFromCodeCoverage]
    private sealed class TestTokenProtector : IWearableTokenProtector {
        public ProtectedWearableToken Protect(string token) => ProtectedWearableToken.FromProtectedValue($"fdp1:{token}");
        public string Unprotect(ProtectedWearableToken protectedToken) => protectedToken.IsProtected
            ? protectedToken.Value[5..]
            : protectedToken.Value;
    }

    [ExcludeFromCodeCoverage]
    private sealed class EmptyWearableClient : IWearableClient {
        public WearableProvider Provider => WearableProvider.Fitbit;
        public string GetAuthorizationUrl(string state) => throw new NotSupportedException();
        public Task<WearableTokenResult?> ExchangeCodeAsync(string code, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<WearableTokenResult?> RefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<Result<IReadOnlyList<WearableDataPoint>>> FetchDailyDataAsync(string accessToken, DateTime date, CancellationToken cancellationToken = default) =>
            Task.FromResult(Result.Success<IReadOnlyList<WearableDataPoint>>([]));
    }
}
