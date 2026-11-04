using FoodDiary.Authentication.Infrastructure;
using FoodDiary.Modules.Identity.Application.Abstractions.Authentication.Abstractions;
using FoodDiary.Modules.Identity.Infrastructure.Authentication;
using FoodDiary.Authentication.Contracts.Authentication.Abstractions;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace FoodDiary.Modules.Identity.Infrastructure.Tests.Authentication;

[ExcludeFromCodeCoverage]
public sealed class AdminSsoProtocolTests {
    [Fact]
    public async Task CreateCodeAsync_PreservesEncodingExpiryPayloadAndCancellationToken() {
        var store = new RecordingStore();
        var time = new MutableTimeProvider();
        var service = new AdminSsoService(store, time);
        var issuer = new AdminSsoIssuer(UserId.New(), Guid.NewGuid(), 7);
        using var cancellation = new CancellationTokenSource();

        AdminSsoCode result = await service.CreateCodeAsync(issuer, cancellation.Token);
        byte[] decoded = Convert.FromBase64String(result.Code.Replace('-', '+').Replace('_', '/') + "=");

        Assert.Multiple(
            () => Assert.Equal(43, result.Code.Length),
            () => Assert.Equal(32, decoded.Length),
            () => Assert.All(result.Code, character => Assert.True(char.IsAsciiLetterOrDigit(character) || character is '-' or '_')),
            () => Assert.Equal(time.UtcNow.UtcDateTime.AddMinutes(2), result.ExpiresAtUtc),
            () => Assert.Equal(result.Code, store.StoredCode),
            () => Assert.Equal(FormattableString.Invariant($"{issuer.UserId.Value:D}:{issuer.SessionId:D}:{issuer.SecurityVersion}"), store.StoredValue),
            () => Assert.Equal(TimeSpan.FromMinutes(2), store.Lifetime),
            () => Assert.Equal(cancellation.Token, store.LastCancellationToken));
    }

    [Theory]
    [InlineData('a', 42)]
    [InlineData('a', 44)]
    [InlineData(' ', 43)]
    [InlineData('=', 43)]
    [InlineData('+', 43)]
    [InlineData('/', 43)]
    [InlineData('я', 43)]
    public async Task ExchangeCodeAsync_RejectsMalformedCodesBeforeTouchingSharedStore(char character, int count) {
        var store = new RecordingStore();
        var service = new AdminSsoService(store, TimeProvider.System);

        AdminSsoIssuer? result = await service.ExchangeCodeAsync(new string(character, count));

        Assert.Multiple(() => Assert.Null(result), () => Assert.Equal(0, store.ConsumeCount));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("not-a-guid")]
    [InlineData("0abf7172-23ee-42d3-a794-56364b61796b")]
    [InlineData("impersonation:test-token")]
    public async Task ExchangeCodeAsync_RejectsMissingOrForeignPayload(string? payload) {
        var store = new RecordingStore { Payload = payload };
        var service = new AdminSsoService(store, TimeProvider.System);
        using var cancellation = new CancellationTokenSource();
        string code = new('a', 43);

        AdminSsoIssuer? result = await service.ExchangeCodeAsync(code, cancellation.Token);

        Assert.Multiple(
            () => Assert.Null(result),
            () => Assert.Equal(1, store.ConsumeCount),
            () => Assert.Equal(code, store.ConsumedCode),
            () => Assert.Equal(cancellation.Token, store.LastCancellationToken));
    }

    [Theory]
    [InlineData(119, true)]
    [InlineData(120, false)]
    [InlineData(121, false)]
    public async Task ExchangeCodeAsync_RespectsSharedStoreExpiryBoundary(int seconds, bool expectedValid) {
        var time = new MutableTimeProvider();
        var services = new ServiceCollection();
        services.AddSingleton<TimeProvider>(time);
        services.AddSharedAuthentication(new ConfigurationBuilder().Build());
        services.AddIdentityAuthenticationInfrastructure();
        await using ServiceProvider provider = services.BuildServiceProvider();
        IAdminSsoService service = provider.GetRequiredService<IAdminSsoService>();
        var issuer = new AdminSsoIssuer(UserId.New(), Guid.NewGuid(), 7);
        AdminSsoCode created = await service.CreateCodeAsync(issuer);
        time.UtcNow = time.UtcNow.AddSeconds(seconds);

        AdminSsoIssuer? first = await service.ExchangeCodeAsync(created.Code);
        AdminSsoIssuer? second = await service.ExchangeCodeAsync(created.Code);

        Assert.Multiple(
            () => Assert.Equal(expectedValid ? issuer : null, first),
            () => Assert.Null(second));
    }

    [Fact]
    public async Task SharedStoreCancellation_PropagatesWithoutConsumingValidCode() {
        var services = new ServiceCollection();
        services.AddSharedAuthentication(new ConfigurationBuilder().Build());
        services.AddIdentityAuthenticationInfrastructure();
        await using ServiceProvider provider = services.BuildServiceProvider();
        IAdminSsoService service = provider.GetRequiredService<IAdminSsoService>();
        var issuer = new AdminSsoIssuer(UserId.New(), Guid.NewGuid(), 7);
        AdminSsoCode created = await service.CreateCodeAsync(issuer);
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => service.CreateCodeAsync(issuer, cancellation.Token));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => service.ExchangeCodeAsync(created.Code, cancellation.Token));
        Assert.Equal(issuer, await service.ExchangeCodeAsync(created.Code));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task AuthenticationRegistration_UsesHostSelectedStoreWithoutOverridingIt(bool replaceBeforeModule) {
        var store = new RecordingStore();
        var services = new ServiceCollection();
        services.AddSharedAuthentication(new ConfigurationBuilder().Build());
        if (replaceBeforeModule) {
            services.Replace(ServiceDescriptor.Singleton<IAdminSsoCodeStore>(store));
        }

        services.AddIdentityAuthenticationInfrastructure();
        if (!replaceBeforeModule) {
            services.Replace(ServiceDescriptor.Singleton<IAdminSsoCodeStore>(store));
        }

        await using ServiceProvider provider = services.BuildServiceProvider();
        IAdminSsoService service = provider.GetRequiredService<IAdminSsoService>();
        var issuer = new AdminSsoIssuer(UserId.New(), Guid.NewGuid(), 7);
        AdminSsoCode result = await service.CreateCodeAsync(issuer);
        store.Payload = store.StoredValue;
        Assert.Equal(issuer, await service.ExchangeCodeAsync(result.Code));
        Assert.Multiple(
            () => Assert.Same(store, provider.GetRequiredService<IAdminSsoCodeStore>()),
            () => Assert.Equal(result.Code, store.StoredCode),
            () => Assert.Equal(result.Code, store.ConsumedCode),
            () => Assert.Single(services, descriptor => descriptor.ServiceType == typeof(IAdminSsoService)),
            () => Assert.Single(services, descriptor => descriptor.ServiceType == typeof(IAdminSsoCodeStore)));
    }

    [ExcludeFromCodeCoverage]
    private sealed class MutableTimeProvider : TimeProvider {
        public DateTimeOffset UtcNow { get; set; } = new(2030, 1, 1, 0, 0, 0, TimeSpan.Zero);
        public override DateTimeOffset GetUtcNow() => UtcNow;
    }

    [ExcludeFromCodeCoverage]
    private sealed class RecordingStore : IAdminSsoCodeStore {
        public string? Payload { get; set; }
        public string? StoredCode { get; private set; }
        public string? StoredValue { get; private set; }
        public TimeSpan Lifetime { get; private set; }
        public string? ConsumedCode { get; private set; }
        public int ConsumeCount { get; private set; }
        public CancellationToken LastCancellationToken { get; private set; }

        public Task StoreAsync(string code, string userId, TimeSpan lifetime, CancellationToken cancellationToken = default) {
            StoredCode = code;
            StoredValue = userId;
            Lifetime = lifetime;
            LastCancellationToken = cancellationToken;
            return Task.CompletedTask;
        }

        public Task<string?> ConsumeAsync(string code, CancellationToken cancellationToken = default) {
            ConsumedCode = code;
            ConsumeCount++;
            LastCancellationToken = cancellationToken;
            return Task.FromResult(Payload);
        }
    }
}
