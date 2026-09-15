using Microsoft.Extensions.DependencyInjection;
using FoodDiary.Mediator;
using FoodDiary.Modules.Notifications.Contracts.Commands.CleanupExpiredNotifications;
using FoodDiary.Modules.Products.Application.Common;
using FoodDiary.Modules.Recipes.Application.Services;

namespace FoodDiary.ArchitectureTests;

[ExcludeFromCodeCoverage]
public sealed class RuntimeRegistrationBoundaryTests {
    [Fact]
    public void RuntimeRegistration_DoesNotRegisterBusinessServices() {
        var services = new ServiceCollection();
        FoodDiary.Application.Runtime.DependencyInjection.AddApplicationRuntime(services);

        Assert.DoesNotContain(services, descriptor => descriptor.ServiceType == typeof(IRequestHandler<CleanupExpiredNotificationsCommand, int>));
        Assert.DoesNotContain(services, descriptor => descriptor.ServiceType == typeof(IProductSearchSuggestionProvider));
        Assert.DoesNotContain(services, descriptor => descriptor.ServiceType == typeof(RecentRecipeLoader));
    }
}
