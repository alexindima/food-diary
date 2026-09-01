using FoodDiary.Application.Abstractions.Authentication.Abstractions;
using FoodDiary.Application.Abstractions.Authentication.Common;
using FoodDiary.Application.Abstractions.Common.Abstractions.Audit;
using FoodDiary.Infrastructure.Authentication;
using FoodDiary.Infrastructure.Persistence.Authentication;
using FoodDiary.Infrastructure.Persistence.Email;
using FoodDiary.Infrastructure.Services;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Infrastructure;

public static partial class DependencyInjection {
    private static void AddAuthenticationInfrastructure(this IServiceCollection services) {
        services.AddSingleton<IJwtTokenGenerator, JwtTokenGenerator>();
        services.AddSingleton<IPasswordHasher, PasswordHasher>();
        services.AddSingleton<IAdminSsoCodeStore, InMemoryAdminSsoCodeStore>();
        services.AddSingleton<IAdminSsoService, AdminSsoService>();
        services.AddScoped<ITelegramAssertionReplayGuard, TelegramAssertionReplayGuard>();
        services.AddSingleton<IEmailTemplateProvider, EmailTemplateProvider>();
        services.AddSingleton<IAuditLogger, StructuredAuditLogger>();

    }
}
