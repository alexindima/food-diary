using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Infrastructure.Persistence;
using FoodDiary.Infrastructure.Persistence.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace FoodDiary.Infrastructure.IntegrationTests.Integration;

[ExcludeFromCodeCoverage]
public sealed class UserSecurityReaderRegistrationTests {
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void AddUsersPersistence_KeepsScopedReaderSeparateFromSharedRepositoryAliases(bool moduleFirst) {
        var services = new ServiceCollection();
        if (moduleFirst) {
            Assert.Same(services, services.AddUsersPersistence());
        }

        services.AddInfrastructure(new ConfigurationBuilder().Build());
        if (!moduleFirst) {
            Assert.Same(services, services.AddUsersPersistence());
        }

        services.Replace(ServiceDescriptor.Scoped(_ => new FoodDiaryDbContext(
            new DbContextOptionsBuilder<FoodDiaryDbContext>()
                .UseNpgsql("Host=localhost;Database=unused;Username=test")
                .Options)));
        using ServiceProvider provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
        using IServiceScope first = provider.CreateScope();
        using IServiceScope second = provider.CreateScope();
        IUserAccessTokenSecurityReader reader = first.ServiceProvider.GetRequiredService<IUserAccessTokenSecurityReader>();
        IUserRepository repository = first.ServiceProvider.GetRequiredService<IUserRepository>();

        Assert.Multiple(
            () => Assert.IsType<UserAccessTokenSecurityReader>(reader),
            () => Assert.Same(typeof(UsersModuleRegistration).Assembly, reader.GetType().Assembly),
            () => Assert.Same(reader, first.ServiceProvider.GetRequiredService<IUserAccessTokenSecurityReader>()),
            () => Assert.NotSame(reader, second.ServiceProvider.GetRequiredService<IUserAccessTokenSecurityReader>()),
            () => Assert.NotSame(reader, repository),
            () => Assert.False(repository is IUserAccessTokenSecurityReader),
            () => Assert.Same(repository, first.ServiceProvider.GetRequiredService<UserRepository>()),
            () => Assert.Same(repository, first.ServiceProvider.GetRequiredService<IUserLookupRepository>()),
            () => Assert.Same(repository, first.ServiceProvider.GetRequiredService<IUserWriteRepository>()),
            () => Assert.Same(repository, first.ServiceProvider.GetRequiredService<IUserGoogleIdentityRepository>()),
            () => Assert.Same(repository, first.ServiceProvider.GetRequiredService<IUserAdminReadRepository>()),
            () => Assert.Same(repository, first.ServiceProvider.GetRequiredService<IUserAdminReadModelRepository>()),
            () => Assert.Single(services, descriptor => descriptor.ServiceType == typeof(IUserAccessTokenSecurityReader)));
    }

    [Fact]
    public void AddInfrastructure_DoesNotOwnSecurityReader() {
        var services = new ServiceCollection();
        services.AddInfrastructure(new ConfigurationBuilder().Build());

        Assert.DoesNotContain(services, descriptor => descriptor.ServiceType == typeof(IUserAccessTokenSecurityReader));
    }
}
