using FoodDiary.Presentation.Api.Filters;
using FoodDiary.Web.Api.Services;
using StackExchange.Redis;

namespace FoodDiary.Web.Api.Tests.Services;

[ExcludeFromCodeCoverage]
public sealed class RedisIdempotencyStoreTests {
    private static readonly TimeSpan ResponseTtl = TimeSpan.FromMinutes(10);
    private static readonly TimeSpan ProcessingTtl = TimeSpan.FromSeconds(30);

    [Fact]
    public async Task ReserveAsync_WhenCompletedResponseMatches_ReturnsReplay() {
        IDatabase database = Substitute.For<IDatabase>();
        database.StringGetAsync(ResponseKey("request-1"), CommandFlags.None)
            .Returns("""{"requestHash":"hash-1","statusCode":201,"body":"created"}""");
        RedisIdempotencyStore store = CreateStore(database);

        IdempotencyReservation reservation = await store.ReserveAsync(
            "request-1",
            "hash-1",
            ResponseTtl,
            ProcessingTtl,
            CancellationToken.None);

        Assert.Multiple(
            () => Assert.Equal(IdempotencyReservationStatus.Replay, reservation.Status),
            () => Assert.Equal(201, reservation.StatusCode),
            () => Assert.Equal("created", reservation.Body));
        await database.DidNotReceive().StringSetAsync(
            Arg.Any<RedisKey>(),
            Arg.Any<RedisValue>(),
            Arg.Any<TimeSpan?>(),
            Arg.Any<When>());
    }

    [Fact]
    public async Task ReserveAsync_WhenCompletedResponseHasDifferentHash_ReturnsConflict() {
        IDatabase database = Substitute.For<IDatabase>();
        database.StringGetAsync(ResponseKey("request-2"), CommandFlags.None)
            .Returns("""{"requestHash":"other-hash","statusCode":200,"body":null}""");
        RedisIdempotencyStore store = CreateStore(database);

        IdempotencyReservation reservation = await store.ReserveAsync(
            "request-2",
            "hash-2",
            ResponseTtl,
            ProcessingTtl,
            CancellationToken.None);

        Assert.Equal(IdempotencyReservationStatus.Conflict, reservation.Status);
        await database.DidNotReceive().StringSetAsync(
            Arg.Any<RedisKey>(),
            Arg.Any<RedisValue>(),
            Arg.Any<TimeSpan?>(),
            Arg.Any<When>());
    }

    [Fact]
    public async Task ReserveAsync_WhenCachedResponseIsInvalid_DeletesItAndAcquiresLock() {
        IDatabase database = Substitute.For<IDatabase>();
        database.StringGetAsync(ResponseKey("request-3"), CommandFlags.None)
            .Returns("not-json");
        database.StringSetAsync(LockKey("request-3"), "hash-3", ProcessingTtl, When.NotExists)
            .Returns(returnThis: true);
        RedisIdempotencyStore store = CreateStore(database);

        IdempotencyReservation reservation = await store.ReserveAsync(
            "request-3",
            "hash-3",
            ResponseTtl,
            ProcessingTtl,
            CancellationToken.None);

        Assert.Equal(IdempotencyReservationStatus.Acquired, reservation.Status);
        await database.Received(1).KeyDeleteAsync(ResponseKey("request-3"), CommandFlags.None);
    }

    [Fact]
    public async Task ReserveAsync_WhenDifferentRequestOwnsActiveLock_ReturnsConflict() {
        IDatabase database = Substitute.For<IDatabase>();
        database.StringSetAsync(LockKey("request-4"), "hash-4", ProcessingTtl, When.NotExists)
            .Returns(returnThis: false);
        database.StringGetAsync(ResponseKey("request-4"), CommandFlags.None)
            .Returns(RedisValue.Null);
        database.StringGetAsync(LockKey("request-4"), CommandFlags.None)
            .Returns("other-hash");
        RedisIdempotencyStore store = CreateStore(database);

        IdempotencyReservation reservation = await store.ReserveAsync(
            "request-4",
            "hash-4",
            ResponseTtl,
            ProcessingTtl,
            CancellationToken.None);

        Assert.Equal(IdempotencyReservationStatus.Conflict, reservation.Status);
    }

    [Fact]
    public async Task ReserveAsync_WhenSameRequestOwnsActiveLock_ReturnsInProgress() {
        IDatabase database = Substitute.For<IDatabase>();
        database.StringSetAsync(LockKey("request-5"), "hash-5", ProcessingTtl, When.NotExists)
            .Returns(returnThis: false);
        database.StringGetAsync(ResponseKey("request-5"), CommandFlags.None)
            .Returns(RedisValue.Null);
        database.StringGetAsync(LockKey("request-5"), CommandFlags.None)
            .Returns("hash-5");
        RedisIdempotencyStore store = CreateStore(database);

        IdempotencyReservation reservation = await store.ReserveAsync(
            "request-5",
            "hash-5",
            ResponseTtl,
            ProcessingTtl,
            CancellationToken.None);

        Assert.Equal(IdempotencyReservationStatus.InProgress, reservation.Status);
    }

    [Fact]
    public async Task ReserveAsync_WhenCompletedResponseAppearsAfterFailedLock_ReturnsReplay() {
        IDatabase database = Substitute.For<IDatabase>();
        database.StringSetAsync(LockKey("request-replay-after-lock"), "hash-replay", ProcessingTtl, When.NotExists)
            .Returns(returnThis: false);
        database.StringGetAsync(ResponseKey("request-replay-after-lock"), CommandFlags.None)
            .Returns(
                RedisValue.Null,
                """{"requestHash":"hash-replay","statusCode":202,"body":"accepted"}""");
        RedisIdempotencyStore store = CreateStore(database);

        IdempotencyReservation reservation = await store.ReserveAsync(
            "request-replay-after-lock",
            "hash-replay",
            ResponseTtl,
            ProcessingTtl,
            CancellationToken.None);

        Assert.Multiple(
            () => Assert.Equal(IdempotencyReservationStatus.Replay, reservation.Status),
            () => Assert.Equal(202, reservation.StatusCode),
            () => Assert.Equal("accepted", reservation.Body));
    }

    [Fact]
    public async Task ReserveAsync_WhenLockExpiresBetweenAttempts_TriesToAcquireAgain() {
        IDatabase database = Substitute.For<IDatabase>();
        database.StringSetAsync(LockKey("request-6"), "hash-6", ProcessingTtl, When.NotExists)
            .Returns(returnThis: false, returnThese: true);
        database.StringGetAsync(ResponseKey("request-6"), CommandFlags.None)
            .Returns(RedisValue.Null);
        database.StringGetAsync(LockKey("request-6"), CommandFlags.None)
            .Returns(RedisValue.Null);
        RedisIdempotencyStore store = CreateStore(database);

        IdempotencyReservation reservation = await store.ReserveAsync(
            "request-6",
            "hash-6",
            ResponseTtl,
            ProcessingTtl,
            CancellationToken.None);

        Assert.Equal(IdempotencyReservationStatus.Acquired, reservation.Status);
        await database.Received(2).StringSetAsync(LockKey("request-6"), "hash-6", ProcessingTtl, When.NotExists);
    }

    [Fact]
    public async Task CompleteAsync_WritesCompletedResponseAndReleasesLock() {
        IDatabase database = Substitute.For<IDatabase>();
        RedisIdempotencyStore store = CreateStore(database);

        await store.CompleteAsync(
            "request-7",
            "hash-7",
            202,
            """{"queued":true}""",
            ResponseTtl,
            CancellationToken.None);

        await database.Received(1).StringSetAsync(
            ResponseKey("request-7"),
            Arg.Is<RedisValue>(value =>
                value.ToString().Contains("\"requestHash\":\"hash-7\"", StringComparison.Ordinal) &&
                value.ToString().Contains("\"statusCode\":202", StringComparison.Ordinal) &&
                value.ToString().Contains("\"body\":\"{\\u0022queued\\u0022:true}\"", StringComparison.Ordinal)),
            ResponseTtl);
        await database.Received(1).LockReleaseAsync(LockKey("request-7"), "hash-7", CommandFlags.None);
    }

    private static RedisIdempotencyStore CreateStore(IDatabase database) {
        IConnectionMultiplexer connectionMultiplexer = Substitute.For<IConnectionMultiplexer>();
        connectionMultiplexer.GetDatabase(Arg.Any<int>(), Arg.Any<object?>()).Returns(database);

        return new RedisIdempotencyStore(connectionMultiplexer);
    }

    private static RedisKey ResponseKey(string key) => "fooddiary:idempotency:" + key + ":response";

    private static RedisKey LockKey(string key) => "fooddiary:idempotency:" + key + ":lock";
}
