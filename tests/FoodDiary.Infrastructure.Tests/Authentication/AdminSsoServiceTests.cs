using FoodDiary.Application.Common.Interfaces.Services;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Infrastructure.Authentication;
using Microsoft.Extensions.Caching.Distributed;

namespace FoodDiary.Infrastructure.Tests.Authentication;

public sealed class AdminSsoServiceTests {
    private static readonly DateTime FixedUtcNow = new(2026, 4, 6, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task CreateCodeAsync_ReturnsNonEmptyCode() {
        var service = CreateService();
        var userId = UserId.New();

        var result = await service.CreateCodeAsync(userId, CancellationToken.None);

        Assert.False(string.IsNullOrWhiteSpace(result.Code));
        Assert.True(result.ExpiresAtUtc > FixedUtcNow);
    }

    [Fact]
    public async Task ExchangeCodeAsync_WithValidCode_ReturnsUserId() {
        var cache = new InMemoryDistributedCache();
        var service = CreateService(cache);
        var userId = UserId.New();

        var created = await service.CreateCodeAsync(userId, CancellationToken.None);
        var exchanged = await service.ExchangeCodeAsync(created.Code, CancellationToken.None);

        Assert.NotNull(exchanged);
        Assert.Equal(userId, exchanged.Value);
    }

    [Fact]
    public async Task ExchangeCodeAsync_WithInvalidCode_ReturnsNull() {
        var service = CreateService();

        var result = await service.ExchangeCodeAsync("invalid-code", CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task ExchangeCodeAsync_WithEmptyCode_ReturnsNull() {
        var service = CreateService();

        var result = await service.ExchangeCodeAsync("", CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task ExchangeCodeAsync_ConsumesCode_SecondExchangeFails() {
        var cache = new InMemoryDistributedCache();
        var service = CreateService(cache);
        var userId = UserId.New();

        var created = await service.CreateCodeAsync(userId, CancellationToken.None);
        await service.ExchangeCodeAsync(created.Code, CancellationToken.None);
        var secondExchange = await service.ExchangeCodeAsync(created.Code, CancellationToken.None);

        Assert.Null(secondExchange);
    }

    private static AdminSsoService CreateService(InMemoryDistributedCache? cache = null) =>
        new(cache ?? new InMemoryDistributedCache(), new StubDateTimeProvider());

    private sealed class StubDateTimeProvider : IDateTimeProvider {
        public DateTime UtcNow => FixedUtcNow;
    }

    private sealed class InMemoryDistributedCache : IDistributedCache {
        private readonly Dictionary<string, byte[]> _store = new();

        public byte[]? Get(string key) => _store.GetValueOrDefault(key);

        public Task<byte[]?> GetAsync(string key, CancellationToken token = default) =>
            Task.FromResult(_store.GetValueOrDefault(key));

        public void Set(string key, byte[] value, DistributedCacheEntryOptions options) =>
            _store[key] = value;

        public Task SetAsync(string key, byte[] value, DistributedCacheEntryOptions options,
            CancellationToken token = default) {
            _store[key] = value;
            return Task.CompletedTask;
        }

        public void Refresh(string key) { }
        public Task RefreshAsync(string key, CancellationToken token = default) => Task.CompletedTask;

        public void Remove(string key) => _store.Remove(key);

        public Task RemoveAsync(string key, CancellationToken token = default) {
            _store.Remove(key);
            return Task.CompletedTask;
        }
    }
}
