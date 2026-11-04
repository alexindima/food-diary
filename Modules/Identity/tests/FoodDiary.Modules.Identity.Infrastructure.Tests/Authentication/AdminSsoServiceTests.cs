using FoodDiary.Authentication.Infrastructure;
using FoodDiary.Modules.Identity.Application.Abstractions.Authentication.Abstractions;
using FoodDiary.Modules.Identity.Infrastructure.Authentication;
using FoodDiary.Authentication.Contracts.Authentication.Abstractions;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
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
        var issuer = new AdminSsoIssuer(UserId.New(), Guid.NewGuid(), 7);

        AdminSsoCode result = await service.CreateCodeAsync(issuer, CancellationToken.None);

        Assert.False(string.IsNullOrWhiteSpace(result.Code));
        Assert.True(result.ExpiresAtUtc > FixedUtcNow);
    }

    [Fact]
    public async Task ExchangeCodeAsync_WithValidCode_ReturnsUserId() {
        AdminSsoService service = CreateService();
        var issuer = new AdminSsoIssuer(UserId.New(), Guid.NewGuid(), 7);

        AdminSsoCode created = await service.CreateCodeAsync(issuer, CancellationToken.None);
        AdminSsoIssuer? exchanged = await service.ExchangeCodeAsync(created.Code, CancellationToken.None);

        Assert.NotNull(exchanged);
        Assert.Equal(issuer, exchanged);
    }

    [Fact]
    public async Task ExchangeCodeAsync_WithInvalidCode_ReturnsNull() {
        AdminSsoService service = CreateService();

        AdminSsoIssuer? result = await service.ExchangeCodeAsync("invalid-code", CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task ExchangeCodeAsync_WithEmptyCode_ReturnsNull() {
        AdminSsoService service = CreateService();

        AdminSsoIssuer? result = await service.ExchangeCodeAsync("", CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task ExchangeCodeAsync_ConsumesCode_SecondExchangeFails() {
        AdminSsoService service = CreateService();
        var issuer = new AdminSsoIssuer(UserId.New(), Guid.NewGuid(), 7);

        AdminSsoCode created = await service.CreateCodeAsync(issuer, CancellationToken.None);
        await service.ExchangeCodeAsync(created.Code, CancellationToken.None);
        AdminSsoIssuer? secondExchange = await service.ExchangeCodeAsync(created.Code, CancellationToken.None);

        Assert.Null(secondExchange);
    }

    [Fact]
    public async Task ExchangeCodeAsync_WithConcurrentConsumers_AllowsExactlyOneExchange() {
        AdminSsoService service = CreateService();
        var issuer = new AdminSsoIssuer(UserId.New(), Guid.NewGuid(), 7);
        AdminSsoCode created = await service.CreateCodeAsync(issuer, CancellationToken.None);

        AdminSsoIssuer?[] results = await Task.WhenAll(Enumerable.Range(0, 32)
            .Select(_ => service.ExchangeCodeAsync(created.Code, CancellationToken.None)));

        AdminSsoIssuer exchanged = Assert.Single(results.OfType<AdminSsoIssuer>());
        Assert.Equal(issuer, exchanged);
    }

    private AdminSsoService CreateService() => new(
        _provider.GetRequiredService<IAdminSsoCodeStore>(), _provider.GetRequiredService<TimeProvider>());

    private static ServiceProvider CreateProvider() {
        var services = new ServiceCollection();
        services.AddSingleton<TimeProvider>(new StubDateTimeProvider());
        services.AddSharedAuthentication(new ConfigurationBuilder().Build());
        return services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
    }

    [ExcludeFromCodeCoverage]
    private sealed class StubDateTimeProvider : TimeProvider {
        public override DateTimeOffset GetUtcNow() => new(FixedUtcNow);
    }

}
