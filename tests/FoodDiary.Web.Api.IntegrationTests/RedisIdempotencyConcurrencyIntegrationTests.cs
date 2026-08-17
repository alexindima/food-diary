using System.Runtime.CompilerServices;
using FoodDiary.Presentation.Api.Filters;
using FoodDiary.Web.Api.Services;
using Microsoft.AspNetCore.Http;
using StackExchange.Redis;
using Testcontainers.Redis;

namespace FoodDiary.Web.Api.IntegrationTests;

[ExcludeFromCodeCoverage]
public sealed class RedisIdempotencyConcurrencyIntegrationTests {
    [RequiresDockerFact]
    public async Task CompleteAsync_WhenLeaseWasReacquired_RejectsStaleOwnerCompletion() {
        await using RedisContainer container = new RedisBuilder("redis:7-alpine").Build();
        await container.StartAsync().ConfigureAwait(false);
        ConnectionMultiplexer connection = await ConnectionMultiplexer.ConnectAsync(
            $"{container.GetConnectionString()},abortConnect=false").ConfigureAwait(false);
        await using ConfiguredAsyncDisposable connectionDisposal = connection.ConfigureAwait(false);
        var store = new RedisIdempotencyStore(connection);
        var staleOwnerProcessingTtl = TimeSpan.FromMilliseconds(150);
        var currentOwnerProcessingTtl = TimeSpan.FromSeconds(5);
        var responseTtl = TimeSpan.FromMinutes(1);

        IdempotencyReservation staleOwner = await store.ReserveAsync(
            "concurrent-request",
            "same-hash",
            responseTtl,
            staleOwnerProcessingTtl).ConfigureAwait(false);
        await WaitUntilAsync(
            async () => !await connection.GetDatabase()
                .KeyExistsAsync("fooddiary:idempotency:concurrent-request:lock")
                .ConfigureAwait(false),
            TimeSpan.FromSeconds(5)).ConfigureAwait(false);
        IdempotencyReservation currentOwner = await store.ReserveAsync(
            "concurrent-request",
            "same-hash",
            responseTtl,
            currentOwnerProcessingTtl).ConfigureAwait(false);

        await store.CompleteAsync(
            "concurrent-request",
            "same-hash",
            staleOwner.OwnerToken!,
            StatusCodes.Status202Accepted,
            """{"owner":"stale"}""",
            "/api/v1/items/stale",
            responseTtl).ConfigureAwait(false);
        IdempotencyReservation whileCurrentOwnerActive = await store.ReserveAsync(
            "concurrent-request",
            "same-hash",
            responseTtl,
            currentOwnerProcessingTtl).ConfigureAwait(false);

        Assert.Equal(IdempotencyReservationStatus.Acquired, currentOwner.Status);
        Assert.Equal(IdempotencyReservationStatus.InProgress, whileCurrentOwnerActive.Status);

        await store.CompleteAsync(
            "concurrent-request",
            "same-hash",
            currentOwner.OwnerToken!,
            StatusCodes.Status201Created,
            """{"owner":"current"}""",
            "/api/v1/items/current",
            responseTtl).ConfigureAwait(false);
        IdempotencyReservation replay = await store.ReserveAsync(
            "concurrent-request",
            "same-hash",
            responseTtl,
            currentOwnerProcessingTtl).ConfigureAwait(false);

        Assert.Multiple(
            () => Assert.Equal(IdempotencyReservationStatus.Replay, replay.Status),
            () => Assert.Equal(StatusCodes.Status201Created, replay.StatusCode),
            () => Assert.Equal("/api/v1/items/current", replay.Location),
            () => Assert.Contains("current", replay.Body, StringComparison.Ordinal),
            () => Assert.DoesNotContain("stale", replay.Body, StringComparison.Ordinal));
    }

    private static async Task WaitUntilAsync(Func<Task<bool>> condition, TimeSpan timeout) {
        DateTime deadlineUtc = DateTime.UtcNow.Add(timeout);
        while (DateTime.UtcNow < deadlineUtc) {
            if (await condition().ConfigureAwait(false)) {
                return;
            }
            await Task.Delay(TimeSpan.FromMilliseconds(20)).ConfigureAwait(false);
        }

        throw new TimeoutException("Redis lease did not expire within the expected timeout.");
    }
}
