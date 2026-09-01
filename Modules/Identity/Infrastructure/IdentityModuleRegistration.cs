using FoodDiary.Application.Abstractions.Admin.Common;
using FoodDiary.Application.Abstractions.Authentication.Common;
using FoodDiary.Infrastructure.Persistence.Admin;
using FoodDiary.Infrastructure.Persistence.Users;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Infrastructure;

public static class IdentityModuleRegistration {
    public static IServiceCollection AddIdentityPersistence(this IServiceCollection services) {
        services.AddScoped<IRefreshTokenSessionRepository, RefreshTokenSessionRepository>();
        services.AddScoped<IRefreshTokenSessionReadRepository>(static provider => provider.GetRequiredService<IRefreshTokenSessionRepository>());
        services.AddScoped<IRefreshTokenSessionWriteRepository>(static provider => provider.GetRequiredService<IRefreshTokenSessionRepository>());
        services.AddScoped<IEmailTemplateRepository, EmailTemplateRepository>();
        services.AddScoped<IEmailTemplateReadRepository>(static provider => provider.GetRequiredService<IEmailTemplateRepository>());
        services.AddScoped<IEmailTemplateReadModelRepository>(static provider => provider.GetRequiredService<IEmailTemplateRepository>());
        services.AddScoped<IEmailTemplateWriteRepository>(static provider => provider.GetRequiredService<IEmailTemplateRepository>());
        return services;
    }
}
