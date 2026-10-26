using System.Net;
using Docker.DotNet.Models;
using FoodDiary.Modules.Identity.Application.Abstractions.Authentication.Models;
using FoodDiary.Modules.Identity.Application.Abstractions.Authentication.Abstractions;
using FoodDiary.Modules.Identity.Infrastructure.Authentication;
using FoodDiary.Web.Api.IntegrationTests.TestInfrastructure;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using MsOptions = Microsoft.Extensions.Options.Options;
using StackExchange.Redis;
using Testcontainers.Redis;

namespace FoodDiary.Web.Api.IntegrationTests;

[ExcludeFromCodeCoverage]
public sealed class RedisPasswordLoginThrottleIntegrationTests {
    [RequiresDockerFact]
    public async Task SharedStore_RejectsConcurrentOwners_TracksFailures_AndClearsAfterSuccessAsync() {
        await using RedisContainer container = CreateContainer();
        await container.StartAsync();
        await using ConnectionMultiplexer connection = await ConnectionMultiplexer.ConnectAsync(container.GetConnectionString());
        IOptions<PasswordLoginThrottleOptions> settings = MsOptions.Create(new PasswordLoginThrottleOptions { FailureThreshold = 2 });
        var firstInstance = new RedisPasswordLoginThrottle(connection, settings);
        var secondInstance = new RedisPasswordLoginThrottle(connection, settings);
        PasswordLoginAttempt[] attempts = await Task.WhenAll(Enumerable.Range(0, 32).Select(index =>
            (index % 2 == 0 ? firstInstance : secondInstance).TryBeginAsync(index % 2 == 0 ? "USER@example.com" : " user@EXAMPLE.com ")));
        PasswordLoginAttempt owner = Assert.Single(attempts, attempt => attempt.IsAcquired);
        Assert.True(await secondInstance.CompleteAsync(owner, PasswordLoginAttemptOutcome.InvalidCredentials));
        Assert.False(await firstInstance.CompleteAsync(owner, PasswordLoginAttemptOutcome.Succeeded));
        PasswordLoginAttempt second = await secondInstance.TryBeginAsync("user@example.com");
        Assert.True(await firstInstance.CompleteAsync(second, PasswordLoginAttemptOutcome.InvalidCredentials));
        PasswordLoginAttempt blocked = await secondInstance.TryBeginAsync("USER@example.com");
        Assert.False(blocked.IsAcquired);
        Assert.InRange(blocked.RetryAfterSeconds, 29, 30);

        await using var factory = new TransportApiWebApplicationFactory();
        await using WebApplicationFactory<Program> app = factory.WithWebHostBuilder(builder => {
            builder.UseSetting("ConnectionStrings:Redis", container.GetConnectionString());
            builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(
                new Dictionary<string, string?>(StringComparer.Ordinal) { ["ConnectionStrings:Redis"] = container.GetConnectionString() }));
        });
        IPasswordLoginThrottle configured = app.Services.GetRequiredService<IPasswordLoginThrottle>();
        Assert.IsType<RedisPasswordLoginThrottle>(configured);
        IConnectionMultiplexer configuredConnection = app.Services.GetRequiredService<IConnectionMultiplexer>();
        Assert.Equal(connection.GetEndPoints().Select(endpoint => endpoint.ToString()),
            configuredConnection.GetEndPoints().Select(endpoint => endpoint.ToString()), StringComparer.Ordinal);
        Assert.Equal(connection.GetDatabase().Database, configuredConnection.GetDatabase().Database);
        PasswordLoginAttempt configuredAttempt = await configured.TryBeginAsync("user@example.com");
        Assert.Equal(owner.Key, configuredAttempt.Key);
        Assert.False(configuredAttempt.IsAcquired);

        IDatabase database = connection.GetDatabase();
        RedisKey key = "fooddiary:password-login:" + owner.Key;
        TimeSpan? ttl = await database.KeyTimeToLiveAsync(key);
        Assert.NotNull(ttl);
        Assert.InRange(ttl.Value.TotalSeconds, 1, 900);
        Assert.DoesNotContain("example", key.ToString(), StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("example", string.Join(';', await database.HashGetAllAsync(key)), StringComparison.OrdinalIgnoreCase);
        foreach (int expectedSeconds in new[] { 60, 120, 240, 300, 300 }) {
            await database.HashSetAsync(key, "blocked", 0);
            PasswordLoginAttempt retry = await firstInstance.TryBeginAsync("user@example.com");
            Assert.True(await firstInstance.CompleteAsync(retry, PasswordLoginAttemptOutcome.InvalidCredentials));
            PasswordLoginAttempt nextCooldown = await secondInstance.TryBeginAsync("user@example.com");
            Assert.InRange(nextCooldown.RetryAfterSeconds, expectedSeconds - 1, expectedSeconds);
        }
        // Move the deadline to the exact admission boundary without waiting for wall time.
        await database.HashSetAsync(key, "blocked", 0);
        PasswordLoginAttempt success = await firstInstance.TryBeginAsync("user@example.com");
        Assert.True(await firstInstance.CompleteAsync(success, PasswordLoginAttemptOutcome.Succeeded));
        Assert.False(await database.KeyExistsAsync(key));
        PasswordLoginAttempt fresh = await secondInstance.TryBeginAsync("user@example.com");
        Assert.True(fresh.IsAcquired);
        Assert.True(await secondInstance.CompleteAsync(fresh, PasswordLoginAttemptOutcome.Abandoned));
        Assert.False(await database.KeyExistsAsync(key));
    }

    [RequiresDockerFact]
    public async Task RedisExpiry_ReleasesAbandonedLeases_AndRemovesIdleFailuresAsync() {
        await using RedisContainer container = CreateContainer();
        await container.StartAsync();
        await using ConnectionMultiplexer connection = await ConnectionMultiplexer.ConnectAsync(container.GetConnectionString());
        var throttle = new RedisPasswordLoginThrottle(connection, MsOptions.Create(new PasswordLoginThrottleOptions {
            FailureThreshold = 1,
            FailureWindowSeconds = 2,
            InitialCooldownSeconds = 1,
            MaximumCooldownSeconds = 1,
            AttemptLeaseSeconds = 1,
        }));
        PasswordLoginAttempt abandoned = await throttle.TryBeginAsync("user@example.com");
        RedisKey key = "fooddiary:password-login:" + abandoned.Key;
        await WaitForExpiryAsync(connection.GetDatabase(), key);
        Assert.False(await throttle.CompleteAsync(abandoned, PasswordLoginAttemptOutcome.InvalidCredentials));
        PasswordLoginAttempt next = await throttle.TryBeginAsync("user@example.com");
        Assert.True(await throttle.CompleteAsync(next, PasswordLoginAttemptOutcome.InvalidCredentials));
        Assert.False((await throttle.TryBeginAsync("user@example.com")).IsAcquired);
        await WaitForExpiryAsync(connection.GetDatabase(), key);
        Assert.True((await throttle.TryBeginAsync("user@example.com")).IsAcquired);
    }

    private static async Task WaitForExpiryAsync(IDatabase database, RedisKey key) {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        while (await database.KeyExistsAsync(key)) {
            await Task.Delay(TimeSpan.FromMilliseconds(20), timeout.Token);
        }
    }

    [RequiresDockerFact]
    public async Task ExpiryAndStaleCompletion_CannotEraseSuccessorStateAsync() {
        await using RedisContainer container = CreateContainer();
        await container.StartAsync();
        await using ConnectionMultiplexer connection = await ConnectionMultiplexer.ConnectAsync(container.GetConnectionString());
        var throttle = new RedisPasswordLoginThrottle(connection, MsOptions.Create(new PasswordLoginThrottleOptions { FailureThreshold = 1 }));
        PasswordLoginAttempt stale = await throttle.TryBeginAsync("user@example.com");
        IDatabase database = connection.GetDatabase();
        RedisKey key = "fooddiary:password-login:" + stale.Key;
        await database.HashSetAsync(key, "lease", 0);
        PasswordLoginAttempt successor = await throttle.TryBeginAsync("user@example.com");
        Assert.True(successor.IsAcquired);
        Assert.False(await throttle.CompleteAsync(stale, PasswordLoginAttemptOutcome.Succeeded));
        Assert.True(await throttle.CompleteAsync(successor, PasswordLoginAttemptOutcome.InvalidCredentials));
        Assert.False((await throttle.TryBeginAsync("user@example.com")).IsAcquired);
        await database.HashSetAsync(key, "expires", 0);
        PasswordLoginAttempt afterExpiry = await throttle.TryBeginAsync("user@example.com");
        Assert.True(afterExpiry.IsAcquired);
        Assert.True(await throttle.CompleteAsync(afterExpiry, PasswordLoginAttemptOutcome.Abandoned));
        Assert.False(await database.KeyExistsAsync(key));
    }

    private static RedisContainer CreateContainer() => new RedisBuilder("redis:7-alpine")
        .WithCreateParameterModifier(parameters => {
            parameters.HostConfig ??= new HostConfig();
            parameters.HostConfig.PortBindings ??= new Dictionary<string, IList<PortBinding>>(StringComparer.Ordinal);
            parameters.HostConfig.PortBindings["6379/tcp"] = [new PortBinding { HostIP = IPAddress.Loopback.ToString(), HostPort = "0" }];
        }).Build();
}
