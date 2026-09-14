using FoodDiary.Persistence.Abstractions;
using FoodDiary.Modules.Users.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using FoodDiary.Infrastructure.Persistence;
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
        services.AddScoped(provider => {
            IModuleContextFactory factory = provider.GetRequiredService<IModuleContextFactory>();
            return factory.CreateModuleContext<UsersDbContext>(options => new UsersDbContext(
                new DbContextOptionsBuilder<UsersDbContext>(options).AddInterceptors(new TelegramIdentityConflictInterceptor()).Options), saveOrder: -100);
        });
        services.TryAddEnumerable(ServiceDescriptor.Scoped<ISaveChangesInterceptor, TelegramIdentityConflictInterceptor>());
        services.AddScoped<UserProfileProjectionService>(provider => new UserProfileProjectionService(
            provider.GetRequiredService<UsersDbContext>().Users, CreateTransactionSynchronizer(provider)));
        services.AddScoped<UserRelatedDataReadService>(provider => new UserRelatedDataReadService(
            provider.GetRequiredService<UsersDbContext>().Users, CreateTransactionSynchronizer(provider)));
        services.AddScoped<IUserFastingReminderReadService>(static provider => provider.GetRequiredService<UserRelatedDataReadService>());
        services.AddScoped<IUserCommentAuthorReadService>(static provider => provider.GetRequiredService<UserRelatedDataReadService>());
        services.AddScoped<ICurrentUserAccessService>(static provider => provider.GetRequiredService<UserProfileProjectionService>());
        services.AddScoped<IUserBillingProfileReadRepository>(static provider => provider.GetRequiredService<UserProfileProjectionService>());
        services.AddScoped<IUserAiProfileReadService>(static provider => provider.GetRequiredService<UserProfileProjectionService>());
        services.AddScoped<IUserDashboardProfileReadService>(static provider => provider.GetRequiredService<UserProfileProjectionService>());
        services.AddScoped<IUserDietologistProfileReadService>(static provider => provider.GetRequiredService<UserProfileProjectionService>());
        services.AddScoped<IUserGamificationProfileReadService>(static provider => provider.GetRequiredService<UserProfileProjectionService>());
        services.AddScoped<IUserHydrationProfileReadService>(static provider => provider.GetRequiredService<UserProfileProjectionService>());
        services.AddScoped<IUserTdeeProfileReadService>(static provider => provider.GetRequiredService<UserProfileProjectionService>());
        services.AddScoped<IUserWeeklyCheckInProfileReadService>(static provider => provider.GetRequiredService<UserProfileProjectionService>());
        services.AddScoped<UserRepository>(provider => new UserRepository(
            provider.GetRequiredService<UsersDbContext>().Users, provider.GetRequiredService<UsersDbContext>().UserRoleAuditEvents, CreateTransactionSynchronizer(provider)));
        services.AddScoped<IUserRepository>(static provider => provider.GetRequiredService<UserRepository>());
        services.AddScoped<IUserLookupRepository>(static provider => provider.GetRequiredService<UserRepository>());
        services.AddScoped<IUserGoogleIdentityRepository>(static provider => provider.GetRequiredService<UserRepository>());
        services.AddScoped<IUserWriteRepository>(static provider => provider.GetRequiredService<UserRepository>());
        services.AddScoped<UserAdministrationReadRepository>(provider => new UserAdministrationReadRepository(
            provider.GetRequiredService<UsersDbContext>().Users, provider.GetRequiredService<UsersDbContext>().UserRoles, CreateTransactionSynchronizer(provider)));
        services.AddScoped<IUserAdminReadRepository>(static provider => provider.GetRequiredService<UserAdministrationReadRepository>());
        services.AddScoped<IUserAdminReadModelRepository>(static provider => provider.GetRequiredService<UserAdministrationReadRepository>());
        services.AddScoped<IUserAccessTokenSecurityReader>(provider => new UserAccessTokenSecurityReader(
            provider.GetRequiredService<UsersDbContext>().Users, CreateTransactionSynchronizer(provider)));
        services.AddScoped<IUserRoleCatalogService>(provider => new UserRoleCatalogService(
            provider.GetRequiredService<UsersDbContext>().Roles, CreateTransactionSynchronizer(provider)));
        services.AddScoped<IUserRoleMembershipService>(provider => new UserRoleMembershipService(
            provider.GetRequiredService<UsersDbContext>().Database, provider.GetRequiredService<UsersDbContext>().Users, provider.GetRequiredService<UsersDbContext>().Roles, provider.GetRequiredService<UsersDbContext>().UserRoles, CreateTransactionSynchronizer(provider)));
        services.AddScoped<IUserCleanupService, UserCleanupService>();
        return services;
    }

    private static Func<CancellationToken, Task> CreateTransactionSynchronizer(IServiceProvider provider) {
        FoodDiaryDbContext shared = provider.GetRequiredService<FoodDiaryDbContext>();
        UsersDbContext owned = provider.GetRequiredService<UsersDbContext>();
        return async cancellationToken => {
            if (owned.Database.IsRelational()) {
                await owned.Database.UseTransactionAsync(shared.Database.CurrentTransaction?.GetDbTransaction(), cancellationToken).ConfigureAwait(false);
            }
        };
    }
}
