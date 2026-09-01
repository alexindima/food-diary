using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Application.Users;
using FoodDiary.Infrastructure.Persistence.Users;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Infrastructure;

public static class UsersModuleRegistration {
    public static IServiceCollection AddUsersModule(this IServiceCollection services) =>
        services.AddUsersApplication().AddUsersPersistence();

    public static IServiceCollection AddUsersPersistence(this IServiceCollection services) {
        services.AddScoped<IUserRoleCatalogService, UserRoleCatalogService>();
        services.AddScoped<IUserRoleMembershipService, UserRoleMembershipService>();
        services.AddScoped<IUserCurrentWeightProvider, UserCurrentWeightProvider>();
        services.AddScoped<IUserCurrentWaistProvider, UserCurrentWaistProvider>();
        services.AddScoped<IUserCleanupService, UserCleanupService>();
        return services;
    }
}
