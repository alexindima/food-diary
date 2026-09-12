using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Application.Users;
using FoodDiary.Infrastructure.Persistence.Users;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace FoodDiary.Infrastructure;

public static class UsersModuleRegistration {
    public static IServiceCollection AddUsersModule(this IServiceCollection services) =>
        services.AddUsersApplication().AddUsersPersistence();

    public static IServiceCollection AddUsersPersistence(this IServiceCollection services) {
        services.TryAddEnumerable(ServiceDescriptor.Scoped<ISaveChangesInterceptor, TelegramIdentityConflictInterceptor>());
        services.AddScoped<UserProfileProjectionService>();
        services.AddScoped<UserRelatedDataReadService>();
        services.AddScoped<IUserFastingReminderReadService>(static provider => provider.GetRequiredService<UserRelatedDataReadService>());
        services.AddScoped<IUserCommentAuthorReadService>(static provider => provider.GetRequiredService<UserRelatedDataReadService>());
        services.AddScoped<ICurrentUserAccessService>(static provider => provider.GetRequiredService<UserProfileProjectionService>());
        services.AddScoped<IUserAiProfileReadService>(static provider => provider.GetRequiredService<UserProfileProjectionService>());
        services.AddScoped<IUserDashboardProfileReadService>(static provider => provider.GetRequiredService<UserProfileProjectionService>());
        services.AddScoped<IUserDietologistProfileReadService>(static provider => provider.GetRequiredService<UserProfileProjectionService>());
        services.AddScoped<IUserGamificationProfileReadService>(static provider => provider.GetRequiredService<UserProfileProjectionService>());
        services.AddScoped<IUserHydrationProfileReadService>(static provider => provider.GetRequiredService<UserProfileProjectionService>());
        services.AddScoped<IUserTdeeProfileReadService>(static provider => provider.GetRequiredService<UserProfileProjectionService>());
        services.AddScoped<IUserWeeklyCheckInProfileReadService>(static provider => provider.GetRequiredService<UserProfileProjectionService>());
        services.AddScoped<UserRepository>();
        services.AddScoped<IUserRepository>(static provider => provider.GetRequiredService<UserRepository>());
        services.AddScoped<IUserLookupRepository>(static provider => provider.GetRequiredService<UserRepository>());
        services.AddScoped<IUserGoogleIdentityRepository>(static provider => provider.GetRequiredService<UserRepository>());
        services.AddScoped<IUserWriteRepository>(static provider => provider.GetRequiredService<UserRepository>());
        services.AddScoped<UserAdministrationReadRepository>();
        services.AddScoped<IUserAdminReadRepository>(static provider => provider.GetRequiredService<UserAdministrationReadRepository>());
        services.AddScoped<IUserAdminReadModelRepository>(static provider => provider.GetRequiredService<UserAdministrationReadRepository>());
        services.AddScoped<IUserAccessTokenSecurityReader, UserAccessTokenSecurityReader>();
        services.AddScoped<IUserRoleCatalogService, UserRoleCatalogService>();
        services.AddScoped<IUserRoleMembershipService, UserRoleMembershipService>();
        services.AddScoped<IUserCurrentWeightProvider, UserCurrentWeightProvider>();
        services.AddScoped<IUserCurrentWaistProvider, UserCurrentWaistProvider>();
        services.AddScoped<IUserCleanupService, UserCleanupService>();
        return services;
    }
}
