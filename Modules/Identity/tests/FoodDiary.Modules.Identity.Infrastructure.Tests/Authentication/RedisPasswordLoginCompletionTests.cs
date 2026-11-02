using FoodDiary.Modules.Identity.Application.Abstractions.Authentication.Models;
using FoodDiary.Modules.Identity.Infrastructure.Authentication;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using NSubstitute;

namespace FoodDiary.Modules.Identity.Infrastructure.Tests.Authentication;

[ExcludeFromCodeCoverage]
public sealed class RedisPasswordLoginCompletionTests {
    [Fact]
    public async Task UnacquiredAttempt_CannotChangeRedisState() {
        IConnectionMultiplexer connection = Substitute.For<IConnectionMultiplexer>();
        var throttle = new RedisPasswordLoginThrottle(connection, Options.Create(new PasswordLoginThrottleOptions()));

        bool completed = await throttle.CompleteAsync(new PasswordLoginAttempt("synthetic-key", OwnerToken: null),
            PasswordLoginAttemptOutcome.Succeeded, CancellationToken.None);

        Assert.False(completed);
        Assert.Empty(connection.ReceivedCalls());
    }
}
