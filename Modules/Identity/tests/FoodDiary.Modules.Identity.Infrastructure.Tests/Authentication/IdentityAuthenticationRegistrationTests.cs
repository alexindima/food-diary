using FoodDiary.Application.Abstractions.Authentication.Abstractions;
using FoodDiary.Application.Abstractions.Authentication.Common;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Infrastructure;
using FoodDiary.Infrastructure.Authentication;
using FoodDiary.Infrastructure.Options;
using FoodDiary.Infrastructure.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace FoodDiary.Modules.Identity.Infrastructure.Tests.Authentication;

[ExcludeFromCodeCoverage]
public sealed class IdentityAuthenticationRegistrationTests {
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void AddIdentityAuthenticationInfrastructure_ComposesInEitherOrderAndKeepsSingletons(bool moduleFirst) {
        var services = new ServiceCollection();
        IConfiguration configuration = new ConfigurationBuilder().Build();
        if (moduleFirst) {
            Assert.Same(services, services.AddIdentityAuthenticationInfrastructure());
        }

        services.AddInfrastructure(configuration);
        if (!moduleFirst) {
            Assert.Same(services, services.AddIdentityAuthenticationInfrastructure());
        }

        services.AddSingleton(CreateOptions());
        using ServiceProvider provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
        using IServiceScope first = provider.CreateScope();
        using IServiceScope second = provider.CreateScope();
        IJwtTokenGenerator tokens = first.ServiceProvider.GetRequiredService<IJwtTokenGenerator>();
        IPasswordHasher hasher = first.ServiceProvider.GetRequiredService<IPasswordHasher>();
        IAdminSsoService sso = first.ServiceProvider.GetRequiredService<IAdminSsoService>();

        Assert.Multiple(
            () => Assert.IsType<JwtTokenGenerator>(tokens),
            () => Assert.IsType<PasswordHasher>(hasher),
            () => Assert.IsType<AdminSsoService>(sso),
            () => Assert.Same(sso, second.ServiceProvider.GetRequiredService<IAdminSsoService>()),
            () => Assert.Same(sso, provider.GetRequiredService<IAdminSsoService>()),
            () => Assert.Same(typeof(IdentityAuthenticationRegistration).Assembly, sso.GetType().Assembly),
            () => Assert.Same(tokens, second.ServiceProvider.GetRequiredService<IJwtTokenGenerator>()),
            () => Assert.Same(hasher, second.ServiceProvider.GetRequiredService<IPasswordHasher>()),
            () => Assert.Same(tokens, provider.GetRequiredService<IJwtTokenGenerator>()),
            () => Assert.Same(hasher, provider.GetRequiredService<IPasswordHasher>()),
            () => Assert.Same(typeof(IdentityAuthenticationRegistration).Assembly, tokens.GetType().Assembly),
            () => Assert.Same(typeof(IdentityAuthenticationRegistration).Assembly, hasher.GetType().Assembly));
    }

    [Fact]
    public void AddIdentityAuthenticationInfrastructure_ResolvesWithoutPersistenceAndPreservesContracts() {
        var storageServices = new ServiceCollection();
        storageServices.AddInfrastructure(new ConfigurationBuilder().Build());
        using ServiceProvider storage = storageServices.BuildServiceProvider();
        var services = new ServiceCollection();
        services.AddSingleton(storage.GetRequiredService<IAdminSsoCodeStore>());
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton(CreateOptions());
        services.AddIdentityAuthenticationInfrastructure();
        using ServiceProvider provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true, ValidateOnBuild = true });
        IJwtTokenGenerator tokens = provider.GetRequiredService<IJwtTokenGenerator>();
        IPasswordHasher hasher = provider.GetRequiredService<IPasswordHasher>();
        var userId = UserId.New();
        var sessionId = Guid.NewGuid();
        string refresh = tokens.GenerateRefreshToken(userId, "test@example.com", ["Admin"], rememberMe: true, refreshSessionId: sessionId);
        (UserId userId, string email, bool rememberMe, Guid? refreshSessionId)? validated = tokens.ValidateToken(refresh);

        Assert.NotNull(validated);
        Assert.Multiple(
            () => Assert.Equal(userId, validated.Value.userId),
            () => Assert.Equal("test@example.com", validated.Value.email),
            () => Assert.True(validated.Value.rememberMe),
            () => Assert.Equal(sessionId, validated.Value.refreshSessionId),
            () => Assert.Null(tokens.ValidateToken(tokens.GenerateAccessToken(userId, "test@example.com", ["User"]))),
            () => Assert.True(hasher.Verify("test-password", hasher.Hash("test-password"))));
    }

    [Fact]
    public void AddInfrastructure_DoesNotOwnIdentityAuthenticationImplementations() {
        var services = new ServiceCollection();
        services.AddInfrastructure(new ConfigurationBuilder().Build());

        Assert.Multiple(
            () => Assert.DoesNotContain(services, descriptor => descriptor.ServiceType == typeof(IJwtTokenGenerator)),
            () => Assert.DoesNotContain(services, descriptor => descriptor.ServiceType == typeof(IAdminSsoService)),
            () => Assert.DoesNotContain(services, descriptor => descriptor.ServiceType == typeof(IPasswordHasher)));
    }

    private static IOptions<JwtOptions> CreateOptions() => Microsoft.Extensions.Options.Options.Create(new JwtOptions {
        SecretKey = "identity-adapter-test-key-only-123456789",
        Issuer = "FoodDiaryTests",
        Audience = "FoodDiaryTestClients",
        ExpirationMinutes = 60,
        RefreshTokenExpirationDays = 7,
        RememberMeRefreshTokenExpirationDays = 90,
    });
}
