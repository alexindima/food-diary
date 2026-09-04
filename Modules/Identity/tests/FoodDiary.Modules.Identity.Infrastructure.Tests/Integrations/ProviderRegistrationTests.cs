using FoodDiary.Application.Abstractions.Authentication.Abstractions;
using FoodDiary.Infrastructure;
using FoodDiary.Integrations.Authentication;
using FoodDiary.Integrations.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace FoodDiary.Modules.Identity.Infrastructure.Tests.Integrations;

[ExcludeFromCodeCoverage]
public sealed class ProviderRegistrationTests {
    [Fact]
    public void AddIdentityProvider_RegistersOwnerSingletonsAndPreservesSuppliedClock() {
        var services = new ServiceCollection();
        var clock = new FixedClock();
        services.AddLogging();
        services.AddSingleton<TimeProvider>(clock);
        Assert.Same(services, services.AddIdentityProvider(new ConfigurationBuilder().Build()));
        using ServiceProvider provider = services.BuildServiceProvider();
        using IServiceScope scope = provider.CreateScope();

        GoogleTokenValidator google = Assert.IsType<GoogleTokenValidator>(provider.GetRequiredService<IGoogleTokenValidator>());
        TelegramAuthValidator telegram = Assert.IsType<TelegramAuthValidator>(provider.GetRequiredService<ITelegramAuthValidator>());
        TelegramLoginWidgetValidator widget = Assert.IsType<TelegramLoginWidgetValidator>(provider.GetRequiredService<ITelegramLoginWidgetValidator>());
        Assert.Multiple(
            () => Assert.Same(google, scope.ServiceProvider.GetRequiredService<IGoogleTokenValidator>()),
            () => Assert.Same(telegram, scope.ServiceProvider.GetRequiredService<ITelegramAuthValidator>()),
            () => Assert.Same(widget, scope.ServiceProvider.GetRequiredService<ITelegramLoginWidgetValidator>()),
            () => Assert.Same(clock, provider.GetRequiredService<TimeProvider>()),
            () => Assert.Equal("FoodDiary.Modules.Identity.Infrastructure", google.GetType().Assembly.GetName().Name),
            () => Assert.Equal(typeof(GoogleTokenValidator).Assembly, typeof(TelegramAuthOptions).Assembly),
            () => Assert.Equal(typeof(GoogleTokenValidator).Assembly, typeof(GoogleAuthOptions).Assembly));
    }

    [Fact]
    public void AddIdentityProvider_BindsOptionsAndProvidesDefaultClock() {
        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>(StringComparer.Ordinal) {
            ["GoogleAuth:ClientId"] = "client-id",
            ["TelegramAuth:BotToken"] = "test-token",
            ["TelegramAuth:AuthTtlSeconds"] = "123",
        }).Build();
        using ServiceProvider provider = new ServiceCollection().AddIdentityProvider(configuration).BuildServiceProvider();
        Assert.Multiple(
            () => Assert.Equal("client-id", provider.GetRequiredService<IOptions<GoogleAuthOptions>>().Value.ClientId),
            () => Assert.Equal("test-token", provider.GetRequiredService<IOptions<TelegramAuthOptions>>().Value.BotToken),
            () => Assert.Equal(123, provider.GetRequiredService<IOptions<TelegramAuthOptions>>().Value.AuthTtlSeconds),
            () => Assert.Same(TimeProvider.System, provider.GetRequiredService<TimeProvider>()));
    }

    [Fact]
    public void AddIdentityProvider_PreservesOptionValidationMessages() {
        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>(StringComparer.Ordinal) {
            ["GoogleAuth:ClientId"] = "invalid client",
            ["TelegramAuth:AuthTtlSeconds"] = "0",
        }).Build();
        using ServiceProvider provider = new ServiceCollection().AddIdentityProvider(configuration).BuildServiceProvider();
        OptionsValidationException google = Assert.Throws<OptionsValidationException>(() => provider.GetRequiredService<IOptions<GoogleAuthOptions>>().Value);
        OptionsValidationException telegram = Assert.Throws<OptionsValidationException>(() => provider.GetRequiredService<IOptions<TelegramAuthOptions>>().Value);
        Assert.Multiple(
            () => Assert.Equal(["GoogleAuth:ClientId must be empty or contain at most 512 non-whitespace characters."], google.Failures, StringComparer.Ordinal),
            () => Assert.Equal(["TelegramAuth:AuthTtlSeconds must be greater than zero."], telegram.Failures, StringComparer.Ordinal));
    }

    [ExcludeFromCodeCoverage]
    private sealed class FixedClock : TimeProvider;
}
