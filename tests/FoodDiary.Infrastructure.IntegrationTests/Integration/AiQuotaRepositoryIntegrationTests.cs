using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using FoodDiary.Application.Abstractions.Ai.Common;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Infrastructure.Persistence;
using FoodDiary.Infrastructure.Persistence.Ai;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Infrastructure.IntegrationTests.Integration;

[Collection(PostgresDatabaseCollection.Name)]
[ExcludeFromCodeCoverage]
public sealed class AiQuotaRepositoryIntegrationTests(PostgresDatabaseFixture databaseFixture) {
    private static readonly DateTime PeriodStartUtc = new(2026, 8, 1, 0, 0, 0, DateTimeKind.Utc);

    [RequiresDockerFact]
    public async Task ReserveAsync_WithTwentyConcurrentRequests_NeverExceedsQuota() {
        (DbContextOptions<FoodDiaryDbContext> options, UserId userId) = await CreateDatabaseAsync();
        var timeProvider = new MutableTimeProvider(new DateTime(2026, 8, 17, 12, 0, 0, DateTimeKind.Utc));

        Task<AiQuotaReservationStatus>[] attempts = [.. Enumerable.Range(0, 20)
            .Select(index => new AiQuotaRepository(options, timeProvider).ReserveAsync(CreateRequest(
                RequestIdFor(index),
                userId,
                inputTokens: 100,
                outputTokens: 50,
                inputLimit: 300,
                outputLimit: 150)))];

        AiQuotaReservationStatus[] statuses = await Task.WhenAll(attempts);

        Assert.Equal(3, statuses.Count(status => status == AiQuotaReservationStatus.Acquired));
        Assert.Equal(17, statuses.Count(status => status == AiQuotaReservationStatus.QuotaExceeded));
    }

    [RequiresDockerFact]
    public async Task ReserveAsync_WithSameConcurrentRequestId_AcquiresExactlyOnce() {
        (DbContextOptions<FoodDiaryDbContext> options, UserId userId) = await CreateDatabaseAsync();
        var timeProvider = new MutableTimeProvider(new DateTime(2026, 8, 17, 12, 0, 0, DateTimeKind.Utc));
        AiQuotaReservationRequest request = CreateRequest(
            RequestIdFor(0),
            userId,
            inputTokens: 100,
            outputTokens: 50,
            inputLimit: 10_000,
            outputLimit: 10_000);

        Task<AiQuotaReservationStatus>[] attempts = [.. Enumerable.Range(0, 20)
            .Select(_ => new AiQuotaRepository(options, timeProvider).ReserveAsync(request))];

        AiQuotaReservationStatus[] statuses = await Task.WhenAll(attempts);

        Assert.Equal(1, statuses.Count(status => status == AiQuotaReservationStatus.Acquired));
        Assert.Equal(19, statuses.Count(status => status == AiQuotaReservationStatus.InProgress));
    }

    [RequiresDockerFact]
    public async Task ReconcileAsync_IsIdempotentAndReturnsUnusedBudget() {
        (DbContextOptions<FoodDiaryDbContext> options, UserId userId) = await CreateDatabaseAsync();
        var timeProvider = new MutableTimeProvider(new DateTime(2026, 8, 17, 12, 0, 0, DateTimeKind.Utc));
        var repository = new AiQuotaRepository(options, timeProvider);
        string firstRequestId = RequestIdFor(0);
        AiQuotaReservationStatus reservation = await repository.ReserveAsync(CreateRequest(
            firstRequestId,
            userId,
            inputTokens: 100,
            outputTokens: 50,
            inputLimit: 100,
            outputLimit: 50));

        var usage = new AiQuotaUsage("nutrition", "gpt-test", 40, 10, 50);
        await repository.ReconcileAsync(firstRequestId, usage);
        await repository.ReconcileAsync(firstRequestId, usage);
        AiQuotaReservationStatus secondReservation = await repository.ReserveAsync(CreateRequest(
            RequestIdFor(1),
            userId,
            inputTokens: 60,
            outputTokens: 40,
            inputLimit: 100,
            outputLimit: 50));

        await using var assertionContext = new FoodDiaryDbContext(options);
        int usageRows = await assertionContext.AiUsages.CountAsync(item => item.UserId == userId);
        Assert.Equal(AiQuotaReservationStatus.Acquired, reservation);
        Assert.Equal(AiQuotaReservationStatus.Acquired, secondReservation);
        Assert.Equal(1, usageRows);
    }

    [RequiresDockerFact]
    public async Task ReserveAsync_WhenPendingReservationExpires_ChargesItConservatively() {
        (DbContextOptions<FoodDiaryDbContext> options, UserId userId) = await CreateDatabaseAsync();
        var timeProvider = new MutableTimeProvider(new DateTime(2026, 8, 17, 12, 0, 0, DateTimeKind.Utc));
        var repository = new AiQuotaRepository(options, timeProvider);
        string firstRequestId = RequestIdFor(0);
        AiQuotaReservationRequest firstRequest = CreateRequest(
            firstRequestId,
            userId,
            inputTokens: 100,
            outputTokens: 50,
            inputLimit: 100,
            outputLimit: 50,
            expiresOnUtc: timeProvider.GetUtcNow().UtcDateTime.AddMinutes(1));

        AiQuotaReservationStatus firstStatus = await repository.ReserveAsync(firstRequest);
        timeProvider.Advance(TimeSpan.FromMinutes(2));
        AiQuotaReservationStatus secondStatus = await repository.ReserveAsync(CreateRequest(
            RequestIdFor(1),
            userId,
            inputTokens: 1,
            outputTokens: 1,
            inputLimit: 100,
            outputLimit: 50));
        AiQuotaReservationStatus repeatedStatus = await repository.ReserveAsync(CreateRequest(
            firstRequestId,
            userId,
            inputTokens: 100,
            outputTokens: 50,
            inputLimit: 100,
            outputLimit: 50));

        Assert.Equal(AiQuotaReservationStatus.Acquired, firstStatus);
        Assert.Equal(AiQuotaReservationStatus.QuotaExceeded, secondStatus);
        Assert.Equal(AiQuotaReservationStatus.Duplicate, repeatedStatus);
    }

    [RequiresDockerFact]
    public async Task ReleaseAsync_AllowsSameRequestToAcquireAgain() {
        (DbContextOptions<FoodDiaryDbContext> options, UserId userId) = await CreateDatabaseAsync();
        var timeProvider = new MutableTimeProvider(new DateTime(2026, 8, 17, 12, 0, 0, DateTimeKind.Utc));
        var repository = new AiQuotaRepository(options, timeProvider);
        AiQuotaReservationRequest request = CreateRequest(
            RequestIdFor(0),
            userId,
            inputTokens: 100,
            outputTokens: 50,
            inputLimit: 100,
            outputLimit: 50);

        AiQuotaReservationStatus firstStatus = await repository.ReserveAsync(request);
        await repository.ReleaseAsync(request.RequestId);
        AiQuotaReservationStatus secondStatus = await repository.ReserveAsync(request);

        Assert.Equal(AiQuotaReservationStatus.Acquired, firstStatus);
        Assert.Equal(AiQuotaReservationStatus.Acquired, secondStatus);
    }

    private async Task<(DbContextOptions<FoodDiaryDbContext> Options, UserId UserId)> CreateDatabaseAsync() {
        string connectionString = await databaseFixture.CreateIsolatedDatabaseAsync();
        DbContextOptions<FoodDiaryDbContext> options = new DbContextOptionsBuilder<FoodDiaryDbContext>()
            .UseNpgsql(connectionString, npgsqlOptions => npgsqlOptions.EnableRetryOnFailure())
            .Options;
        await using var context = new FoodDiaryDbContext(options);
        await context.Database.MigrateAsync();
        var user = User.Create($"ai-quota-{Guid.NewGuid():N}@example.com", "hash");
        context.Users.Add(user);
        await context.SaveChangesAsync();
        return (options, user.Id);
    }

    private static AiQuotaReservationRequest CreateRequest(
        string requestId,
        UserId userId,
        long inputTokens,
        long outputTokens,
        long inputLimit,
        long outputLimit,
        DateTime? expiresOnUtc = null) =>
        new(
            requestId,
            userId,
            PeriodStartUtc,
            "nutrition",
            inputTokens,
            outputTokens,
            inputLimit,
            outputLimit,
            expiresOnUtc ?? new DateTime(2026, 8, 17, 12, 15, 0, DateTimeKind.Utc));

    private static string RequestIdFor(int index) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(
            $"ai-quota-request-{index.ToString(CultureInfo.InvariantCulture)}")));

    [ExcludeFromCodeCoverage]
    private sealed class MutableTimeProvider(DateTime utcNow) : TimeProvider {
        private DateTime _utcNow = utcNow;

        public override DateTimeOffset GetUtcNow() => new(_utcNow);

        public void Advance(TimeSpan duration) {
            _utcNow = _utcNow.Add(duration);
        }
    }
}
