using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Application.Users;
using FoodDiary.Application.Users.Common;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Application.Tests.Users;

[ExcludeFromCodeCoverage]
public sealed class UsersDependencyInjectionTests {
    [Fact]
    public void AddUsersModule_ResolvesGamificationAndWeeklyCheckInProfilesFromSharedContext() {
        var services = new ServiceCollection();
        services.AddUsersModule();
        var context = new UserContextService(
            Substitute.For<IUserLookupRepository>(),
            Substitute.For<IUserWriteRepository>());
        var provider = new SingleServiceProvider(context);

        IUserGamificationProfileReadService gamification = ResolveFactory<IUserGamificationProfileReadService>(services, provider);
        IUserWeeklyCheckInProfileReadService weeklyCheckIn = ResolveFactory<IUserWeeklyCheckInProfileReadService>(services, provider);

        Assert.Same(gamification, weeklyCheckIn);
    }

    private static TService ResolveFactory<TService>(
        IServiceCollection services,
        IServiceProvider provider) where TService : class {
        ServiceDescriptor descriptor = Assert.Single(services, item => item.ServiceType == typeof(TService));
        return Assert.IsAssignableFrom<TService>(descriptor.ImplementationFactory!(provider));
    }

    [ExcludeFromCodeCoverage]
    private sealed class SingleServiceProvider(object service) : IServiceProvider {
        public object? GetService(Type serviceType) => serviceType.IsInstanceOfType(service) ? service : null;
    }
}
