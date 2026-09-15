using FoodDiary.Application.Abstractions.Authentication.Abstractions;
using FoodDiary.Application.Abstractions.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace FoodDiary.Authentication.Infrastructure.Tests;

[ExcludeFromCodeCoverage]
public sealed class AuthenticationRegistrationTests {
    [Fact]
    public async Task Store_ConsumesOnlyOnceAndExpiresAtBoundary() {
        var clock = new Clock();
        var services = new ServiceCollection();
        services.AddSingleton<TimeProvider>(clock);
        services.AddSharedAuthentication(new ConfigurationBuilder().Build());
        await using ServiceProvider provider = services.BuildServiceProvider();
        IAdminSsoCodeStore store = provider.GetRequiredService<IAdminSsoCodeStore>();
        Assert.Same(clock, provider.GetRequiredService<TimeProvider>());
        Assert.Same(store, provider.GetRequiredService<IAdminSsoCodeStore>());
        await store.StoreAsync("one", "user", TimeSpan.FromMinutes(2));
        Assert.Equal("user", await store.ConsumeAsync("one"));
        Assert.Null(await store.ConsumeAsync("one"));
        await store.StoreAsync("expired", "user", TimeSpan.FromMinutes(2));
        clock.Now += TimeSpan.FromMinutes(2);
        Assert.Null(await store.ConsumeAsync("expired"));
        using var cancelled = new CancellationTokenSource();
        await cancelled.CancelAsync();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => store.StoreAsync("cancelled", "user", TimeSpan.FromMinutes(1), cancelled.Token));
        Assert.Null(await store.ConsumeAsync("cancelled"));
    }

    [Fact]
    public void Registration_PreservesHostStoreAndRejectsInvalidJwt() {
        var services = new ServiceCollection();
        var hostStore = new HostStore();
        services.AddSingleton<IAdminSsoCodeStore>(hostStore);
        services.AddSharedAuthentication(new ConfigurationBuilder().Build());
        using ServiceProvider provider = services.BuildServiceProvider();
        Assert.Same(hostStore, provider.GetRequiredService<IAdminSsoCodeStore>());
        OptionsValidationException error = Assert.Throws<OptionsValidationException>(() => provider.GetRequiredService<IOptions<JwtOptions>>().Value);
        Assert.Contains("Jwt:SecretKey", error.Message, StringComparison.Ordinal);
    }

    [ExcludeFromCodeCoverage]
    private sealed class Clock : TimeProvider {
        public DateTimeOffset Now { get; set; } = new(2026, 9, 15, 0, 0, 0, TimeSpan.Zero);
        public override DateTimeOffset GetUtcNow() => Now;
    }

    [ExcludeFromCodeCoverage]
    private sealed class HostStore : IAdminSsoCodeStore {
        public Task StoreAsync(string code, string userId, TimeSpan lifetime, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<string?> ConsumeAsync(string code, CancellationToken cancellationToken = default) => Task.FromResult<string?>(null);
    }
}
