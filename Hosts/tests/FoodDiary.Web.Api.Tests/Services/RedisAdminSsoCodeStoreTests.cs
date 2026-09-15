using FoodDiary.Web.Api.Services;
using StackExchange.Redis;

namespace FoodDiary.Web.Api.Tests.Services;

[ExcludeFromCodeCoverage]
public sealed class RedisAdminSsoCodeStoreTests {
    [Fact]
    public async Task StoreAsync_WritesCodeWithExpiration() {
        IDatabase database = Substitute.For<IDatabase>();
        RedisAdminSsoCodeStore store = CreateStore(database);
        var lifetime = TimeSpan.FromMinutes(2);

        await store.StoreAsync("code-1", "user-1", lifetime, CancellationToken.None);

        await database.Received(1).StringSetAsync(
            "fooddiary:admin-sso:code-1",
            "user-1",
            lifetime);
    }

    [Fact]
    public async Task ConsumeAsync_UsesAtomicGetDelete() {
        IDatabase database = Substitute.For<IDatabase>();
        database.StringGetDeleteAsync("fooddiary:admin-sso:code-2", CommandFlags.None)
            .Returns(Task.FromResult((RedisValue)"user-2"));
        RedisAdminSsoCodeStore store = CreateStore(database);

        string? result = await store.ConsumeAsync("code-2", CancellationToken.None);

        Assert.Equal("user-2", result);
        await database.Received(1).StringGetDeleteAsync(
            "fooddiary:admin-sso:code-2",
            CommandFlags.None);
    }

    private static RedisAdminSsoCodeStore CreateStore(IDatabase database) {
        IConnectionMultiplexer connectionMultiplexer = Substitute.For<IConnectionMultiplexer>();
        connectionMultiplexer.GetDatabase(Arg.Any<int>(), Arg.Any<object?>()).Returns(database);
        return new RedisAdminSsoCodeStore(connectionMultiplexer);
    }
}
