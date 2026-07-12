using FoodDiary.Application.Abstractions.Ai.Common;
using FoodDiary.Application.Abstractions.Authentication.Abstractions;
using FoodDiary.Application.Abstractions.Authentication.Common;
using FoodDiary.Application.Abstractions.Common.Abstractions.Audit;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Infrastructure.Authentication;
using FoodDiary.Infrastructure.Persistence.Ai;
using FoodDiary.Infrastructure.Persistence.Email;
using FoodDiary.Infrastructure.Persistence.Users;
using FoodDiary.Infrastructure.Services;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Infrastructure;

public static partial class DependencyInjection {
    private static IServiceCollection AddAuthenticationInfrastructure(this IServiceCollection services) {
        services.AddSingleton<IJwtTokenGenerator, JwtTokenGenerator>();
        services.AddSingleton<IPasswordHasher, PasswordHasher>();
        services.AddSingleton<IAdminSsoService, AdminSsoService>();
        services.AddScoped<IUserCleanupService, UserCleanupService>();
        services.AddSingleton<IEmailTemplateProvider, EmailTemplateProvider>();
        services.AddSingleton<IAiPromptProvider, AiPromptProvider>();
        services.AddSingleton<IAuditLogger, StructuredAuditLogger>();

        return services;
    }
}
