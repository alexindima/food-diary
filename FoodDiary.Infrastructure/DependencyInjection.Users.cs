using FoodDiary.Application.Abstractions.Admin.Common;
using FoodDiary.Application.Abstractions.Authentication.Common;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Infrastructure.Persistence.Admin;
using FoodDiary.Infrastructure.Persistence.Users;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Infrastructure;

public static partial class DependencyInjection {
    private static void AddUserPersistence(this IServiceCollection services) {
        services.AddScoped<UserRepository>();
        services.AddScoped<IUserRepository>(static provider => provider.GetRequiredService<UserRepository>());
        services.AddScoped<IUserLookupRepository>(static provider => provider.GetRequiredService<UserRepository>());
        services.AddScoped<IUserGoogleIdentityRepository>(static provider => provider.GetRequiredService<UserRepository>());
        services.AddScoped<IUserAdminReadRepository>(static provider => provider.GetRequiredService<UserRepository>());
        services.AddScoped<IUserAdminReadModelRepository>(static provider => provider.GetRequiredService<UserRepository>());
        services.AddScoped<IUserWriteRepository>(static provider => provider.GetRequiredService<UserRepository>());
        services.AddScoped<IUserAccessTokenSecurityReader>(static provider => provider.GetRequiredService<UserRepository>());
        services.AddScoped<IUserLoginEventRepository, UserLoginEventRepository>();
        services.AddScoped<IUserLoginEventReadRepository>(static provider => provider.GetRequiredService<IUserLoginEventRepository>());
        services.AddScoped<IUserLoginEventWriteRepository>(static provider => provider.GetRequiredService<IUserLoginEventRepository>());

        services.AddScoped<IRefreshTokenSessionRepository, RefreshTokenSessionRepository>();
        services.AddScoped<IRefreshTokenSessionReadRepository>(static provider => provider.GetRequiredService<IRefreshTokenSessionRepository>());
        services.AddScoped<IRefreshTokenSessionWriteRepository>(static provider => provider.GetRequiredService<IRefreshTokenSessionRepository>());

        services.AddScoped<IAdminUserRoleAuditRepository, AdminUserRoleAuditRepository>();
        services.AddScoped<IAdminUserRoleAuditReadRepository>(static provider => provider.GetRequiredService<IAdminUserRoleAuditRepository>());
        services.AddScoped<IEmailTemplateRepository, EmailTemplateRepository>();
        services.AddScoped<IEmailTemplateReadRepository>(static provider => provider.GetRequiredService<IEmailTemplateRepository>());
        services.AddScoped<IEmailTemplateReadModelRepository>(static provider => provider.GetRequiredService<IEmailTemplateRepository>());
        services.AddScoped<IEmailTemplateWriteRepository>(static provider => provider.GetRequiredService<IEmailTemplateRepository>());

    }
}
