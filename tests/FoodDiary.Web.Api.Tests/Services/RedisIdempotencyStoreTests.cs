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
        ConfigureReservationResult(
            database,
            CompletedResult("""{"requestHash":"hash-1","statusCode":201,"body":"created"}"""));
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
    }

    [Fact]
    public async Task ReserveAsync_WhenCompletedResponseHasDifferentHash_ReturnsConflict() {
        IDatabase database = Substitute.For<IDatabase>();
        ConfigureReservationResult(
            database,
            CompletedResult("""{"requestHash":"other-hash","statusCode":200,"body":null}"""));
        RedisIdempotencyStore store = CreateStore(database);

        IdempotencyReservation reservation = await store.ReserveAsync(
            "request-2",
            "hash-2",
            ResponseTtl,
            ProcessingTtl,
            CancellationToken.None);

        Assert.Equal(IdempotencyReservationStatus.Conflict, reservation.Status);
    }

    [Fact]
    public async Task ReserveAsync_WhenCachedResponseIsInvalid_DeletesItAndAcquiresLock() {
        IDatabase database = Substitute.For<IDatabase>();
        ConfigureReservationResult(database, CompletedResult("not-json"), AcquiredResult());
        RedisIdempotencyStore store = CreateStore(database);

        IdempotencyReservation reservation = await store.ReserveAsync(
            "request-3",
            "hash-3",
            ResponseTtl,
            ProcessingTtl,
            CancellationToken.None);

        Assert.Equal(IdempotencyReservationStatus.Acquired, reservation.Status);
        await database.Received(1).ScriptEvaluateAsync(
            Arg.Is<string>(script => script.Contains("~= ARGV[1]", StringComparison.Ordinal)),
            Arg.Is<RedisKey[]>(keys => keys != null && Enumerable.SequenceEqual(keys, new[] { ResponseKey("request-3") })),
            Arg.Is<RedisValue[]>(values => values != null && Enumerable.SequenceEqual(values, new RedisValue[] { "not-json" })),
            CommandFlags.None);
    }

    [Fact]
    public async Task ReserveAsync_WhenDifferentRequestOwnsActiveLock_ReturnsConflict() {
        IDatabase database = Substitute.For<IDatabase>();
        ConfigureReservationResult(database, ActiveLockResult("other-hash:owner"));
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
        ConfigureReservationResult(database, ActiveLockResult("hash-5:owner"));
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
    public async Task ReserveAsync_WhenNoCompletedResponseOrLockExist_AcquiresAtomically() {
        IDatabase database = Substitute.For<IDatabase>();
        ConfigureReservationResult(database, AcquiredResult());
        RedisIdempotencyStore store = CreateStore(database);

        IdempotencyReservation reservation = await store.ReserveAsync(
            "request-6",
            "hash-6",
            ResponseTtl,
            ProcessingTtl,
            CancellationToken.None);

        Assert.Multiple(
            () => Assert.Equal(IdempotencyReservationStatus.Acquired, reservation.Status),
            () => Assert.NotEmpty(reservation.OwnerToken!));
        await database.Received(1).ScriptEvaluateAsync(
            Arg.Is<string>(script =>
                script.Contains("local response = redis.call('GET', KEYS[1])", StringComparison.Ordinal) &&
                script.Contains("redis.call('SET', KEYS[2]", StringComparison.Ordinal)),
            Arg.Is<RedisKey[]>(keys => keys != null && Enumerable.SequenceEqual(
                keys,
                new[] { ResponseKey("request-6"), LockKey("request-6") })),
            Arg.Is<RedisValue[]>(values => values != null &&
                values[0].ToString().StartsWith("hash-6:", StringComparison.Ordinal) &&
                values[1] == ((long)ProcessingTtl.TotalMilliseconds).ToString(System.Globalization.CultureInfo.InvariantCulture)),
            CommandFlags.None);
    }

    [Fact]
    public async Task CompleteAsync_WritesCompletedResponseAndReleasesLock() {
        IDatabase database = Substitute.For<IDatabase>();
        RedisIdempotencyStore store = CreateStore(database);

        await store.CompleteAsync(
            "request-7",
            "hash-7",
            "owner-7",
            202,
            """{"queued":true}""",
            "/api/v1/queued/7",
            ResponseTtl,
            CancellationToken.None);

        await database.Received(1).ScriptEvaluateAsync(
            Arg.Any<string>(),
            Arg.Is<RedisKey[]>(keys => keys != null &&
                Enumerable.SequenceEqual(keys, new[] { LockKey("request-7"), ResponseKey("request-7") })),
            Arg.Is<RedisValue[]>(values => values != null &&
                values[0] == "hash-7:owner-7" &&
                values[1].ToString().Contains("\"requestHash\":\"hash-7\"", StringComparison.Ordinal) &&
                values[1].ToString().Contains("\"statusCode\":202", StringComparison.Ordinal) &&
                values[1].ToString().Contains("\"location\":\"/api/v1/queued/7\"", StringComparison.Ordinal)),
            CommandFlags.None);
    }

    [Fact]
    public async Task ReleaseAsync_DeletesOnlyTheOwnedLock() {
        IDatabase database = Substitute.For<IDatabase>();
        RedisIdempotencyStore store = CreateStore(database);

        await store.ReleaseAsync(
            "request-release",
            "hash-release",
            "owner-release",
            CancellationToken.None);

        await database.Received(1).ScriptEvaluateAsync(
            Arg.Is<string>(script => script.Contains("redis.call('DEL', KEYS[1])", StringComparison.Ordinal)),
            Arg.Is<RedisKey[]>(keys => keys != null &&
                Enumerable.SequenceEqual(keys, new[] { LockKey("request-release") })),
            Arg.Is<RedisValue[]>(values => values != null &&
                Enumerable.SequenceEqual(values, new RedisValue[] { "hash-release:owner-release" })),
            CommandFlags.None);
    }

    [Fact]
    public async Task ReleaseAsync_WithCancelledToken_DoesNotAccessRedis() {
        IDatabase database = Substitute.For<IDatabase>();
        RedisIdempotencyStore store = CreateStore(database);
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();

        await Assert.ThrowsAsync<OperationCanceledException>(() => store.ReleaseAsync(
            "request-release",
            "hash-release",
            "owner-release",
            cancellation.Token));

        await database.DidNotReceiveWithAnyArgs().ScriptEvaluateAsync(
            default(string)!,
            default(RedisKey[])!,
            default(RedisValue[])!,
            default);
    }

    private static void ConfigureReservationResult(IDatabase database, params RedisResult[] results) {
        var queuedResults = new Queue<RedisResult>(results);
        database.ScriptEvaluateAsync(
                Arg.Any<string>(),
                Arg.Any<RedisKey[]>(),
                Arg.Any<RedisValue[]>(),
                CommandFlags.None)
            .Returns(callInfo => Task.FromResult(
                callInfo.ArgAt<string>(0).Contains("return {3, ''}", StringComparison.Ordinal)
                    ? queuedResults.Dequeue()
                    : RedisResult.Create((RedisValue)1)));
    }

    private static RedisResult CompletedResult(string response) => ReservationResult(1, response);

    private static RedisResult ActiveLockResult(string lockValue) => ReservationResult(2, lockValue);

    private static RedisResult AcquiredResult() => ReservationResult(3, string.Empty);

    private static RedisResult ReservationResult(int state, string value) => RedisResult.Create([
        RedisResult.Create((RedisValue)state),
        RedisResult.Create((RedisValue)value),
    ]);

    private static RedisIdempotencyStore CreateStore(IDatabase database) {
        IConnectionMultiplexer connectionMultiplexer = Substitute.For<IConnectionMultiplexer>();
        connectionMultiplexer.GetDatabase(Arg.Any<int>(), Arg.Any<object?>()).Returns(database);

        return new RedisIdempotencyStore(connectionMultiplexer);
    }

    private static RedisKey ResponseKey(string key) => "fooddiary:idempotency:" + key + ":response";

    private static RedisKey LockKey(string key) => "fooddiary:idempotency:" + key + ":lock";
}
