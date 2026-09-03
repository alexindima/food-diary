using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Infrastructure.Persistence;
using FoodDiary.Infrastructure.Persistence.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace FoodDiary.Infrastructure.IntegrationTests.Integration;

[ExcludeFromCodeCoverage]
public sealed class UserAdministrationReaderRegistrationTests {
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void AddUsersPersistence_SeparatesReadAliasesFromTrackedRepositoryInEitherOrder(bool moduleFirst) {
        var services = new ServiceCollection();
        if (moduleFirst) {
            services.AddUsersPersistence();
        }

        services.AddInfrastructure(new ConfigurationBuilder().Build());
        if (!moduleFirst) {
            services.AddUsersPersistence();
        }

        services.Replace(ServiceDescriptor.Scoped(_ => new FoodDiaryDbContext(
            new DbContextOptionsBuilder<FoodDiaryDbContext>()
                .UseNpgsql("Host=localhost;Database=unused;Username=test")
                .Options)));
        using ServiceProvider provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
        using IServiceScope first = provider.CreateScope();
        using IServiceScope second = provider.CreateScope();
        IUserRepository repository = first.ServiceProvider.GetRequiredService<IUserRepository>();
        IUserAdminReadRepository reader = first.ServiceProvider.GetRequiredService<IUserAdminReadRepository>();
        IUserAdminReadModelRepository models = first.ServiceProvider.GetRequiredService<IUserAdminReadModelRepository>();

        Assert.Multiple(
            () => Assert.IsType<UserAdministrationReadRepository>(reader),
            () => Assert.Same(typeof(UsersModuleRegistration).Assembly, reader.GetType().Assembly),
            () => Assert.Same(reader, models),
            () => Assert.Same(reader, first.ServiceProvider.GetRequiredService<UserAdministrationReadRepository>()),
            () => Assert.NotSame(reader, second.ServiceProvider.GetRequiredService<IUserAdminReadRepository>()),
            () => Assert.NotSame(reader, repository),
            () => Assert.NotSame(reader, first.ServiceProvider.GetRequiredService<IUserAccessTokenSecurityReader>()),
            () => Assert.False(repository is IUserAdminReadRepository),
            () => Assert.False(repository is IUserAdminReadModelRepository),
            () => Assert.Same(repository, first.ServiceProvider.GetRequiredService<UserRepository>()),
            () => Assert.Same(typeof(UsersModuleRegistration).Assembly, repository.GetType().Assembly),
            () => Assert.NotSame(repository, second.ServiceProvider.GetRequiredService<IUserRepository>()),
            () => Assert.Same(repository, first.ServiceProvider.GetRequiredService<IUserLookupRepository>()),
            () => Assert.Same(repository, first.ServiceProvider.GetRequiredService<IUserWriteRepository>()),
            () => Assert.Same(repository, first.ServiceProvider.GetRequiredService<IUserGoogleIdentityRepository>()),
            () => Assert.Single(services, descriptor => descriptor.ServiceType == typeof(IUserAdminReadRepository)),
            () => Assert.Single(services, descriptor => descriptor.ServiceType == typeof(IUserAdminReadModelRepository)));
        foreach (Type port in new[] { typeof(UserRepository), typeof(IUserRepository), typeof(IUserLookupRepository), typeof(IUserGoogleIdentityRepository), typeof(IUserWriteRepository) }) {
            ServiceDescriptor registration = Assert.Single(services, descriptor => descriptor.ServiceType == port);
            Assert.Equal(ServiceLifetime.Scoped, registration.Lifetime);
        }
    }

    [Fact]
    public void AddInfrastructure_DoesNotOwnUsersRepositoryOrReadAliases() {
        var services = new ServiceCollection();
        services.AddInfrastructure(new ConfigurationBuilder().Build());

        Assert.DoesNotContain(services, descriptor => descriptor.ServiceType == typeof(IUserAdminReadRepository));
        Assert.DoesNotContain(services, descriptor => descriptor.ServiceType == typeof(IUserAdminReadModelRepository));
        foreach (Type port in new[] { typeof(UserRepository), typeof(IUserRepository), typeof(IUserLookupRepository), typeof(IUserGoogleIdentityRepository), typeof(IUserWriteRepository), typeof(IUserAccessTokenSecurityReader) }) {
            Assert.DoesNotContain(services, descriptor => descriptor.ServiceType == port);
        }
    }
}
