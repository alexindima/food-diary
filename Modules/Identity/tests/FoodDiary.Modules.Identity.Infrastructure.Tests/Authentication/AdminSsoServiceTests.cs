using FoodDiary.Application.Abstractions.Authentication.Abstractions;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Infrastructure.Authentication;
using FoodDiary.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Modules.Identity.Infrastructure.Tests.Authentication;

[ExcludeFromCodeCoverage]
public sealed class AdminSsoServiceTests : IDisposable {
    private static readonly DateTime FixedUtcNow = new(2026, 4, 6, 12, 0, 0, DateTimeKind.Utc);

    private readonly ServiceProvider _provider = CreateProvider();

    public void Dispose() => _provider.Dispose();

    [Fact]
    public async Task CreateCodeAsync_ReturnsNonEmptyCode() {
        AdminSsoService service = CreateService();
        var userId = UserId.New();

        AdminSsoCode result = await service.CreateCodeAsync(userId, CancellationToken.None);

        Assert.False(string.IsNullOrWhiteSpace(result.Code));
        Assert.True(result.ExpiresAtUtc > FixedUtcNow);
    }

    [Fact]
    public async Task ExchangeCodeAsync_WithValidCode_ReturnsUserId() {
        AdminSsoService service = CreateService();
        var userId = UserId.New();

        AdminSsoCode created = await service.CreateCodeAsync(userId, CancellationToken.None);
        UserId? exchanged = await service.ExchangeCodeAsync(created.Code, CancellationToken.None);

        Assert.NotNull(exchanged);
        Assert.Equal(userId, exchanged.Value);
    }

    [Fact]
    public async Task ExchangeCodeAsync_WithInvalidCode_ReturnsNull() {
        AdminSsoService service = CreateService();

        UserId? result = await service.ExchangeCodeAsync("invalid-code", CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task ExchangeCodeAsync_WithEmptyCode_ReturnsNull() {
        AdminSsoService service = CreateService();

        UserId? result = await service.ExchangeCodeAsync("", CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task ExchangeCodeAsync_ConsumesCode_SecondExchangeFails() {
        AdminSsoService service = CreateService();
        var userId = UserId.New();

        AdminSsoCode created = await service.CreateCodeAsync(userId, CancellationToken.None);
        await service.ExchangeCodeAsync(created.Code, CancellationToken.None);
        UserId? secondExchange = await service.ExchangeCodeAsync(created.Code, CancellationToken.None);

        Assert.Null(secondExchange);
    }

    [Fact]
    public async Task ExchangeCodeAsync_WithConcurrentConsumers_AllowsExactlyOneExchange() {
        AdminSsoService service = CreateService();
        var userId = UserId.New();
        AdminSsoCode created = await service.CreateCodeAsync(userId, CancellationToken.None);

        UserId?[] results = await Task.WhenAll(Enumerable.Range(0, 32)
            .Select(_ => service.ExchangeCodeAsync(created.Code, CancellationToken.None)));

        UserId exchanged = Assert.Single(results.OfType<UserId>());
        Assert.Equal(userId, exchanged);
    }

    private AdminSsoService CreateService() => new(
        _provider.GetRequiredService<IAdminSsoCodeStore>(), _provider.GetRequiredService<TimeProvider>());

    private static ServiceProvider CreateProvider() {
        var services = new ServiceCollection();
        services.AddSingleton<TimeProvider>(new StubDateTimeProvider());
        services.AddInfrastructure(new ConfigurationBuilder().Build());
        return services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
    }

    [ExcludeFromCodeCoverage]
    private sealed class StubDateTimeProvider : TimeProvider {
        public override DateTimeOffset GetUtcNow() => new(FixedUtcNow);
    }

}
