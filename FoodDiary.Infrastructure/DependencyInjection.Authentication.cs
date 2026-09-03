using FoodDiary.Application.Abstractions.Authentication.Abstractions;
using FoodDiary.Application.Abstractions.Common.Abstractions.Audit;
using FoodDiary.Infrastructure.Authentication;
using FoodDiary.Infrastructure.Services;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Infrastructure;

public static partial class DependencyInjection {
    private static void AddAuthenticationInfrastructure(this IServiceCollection services) {
        services.AddSingleton<IAdminSsoCodeStore, InMemoryAdminSsoCodeStore>();
        services.AddSingleton<IAdminSsoService, AdminSsoService>();
        services.AddSingleton<IAuditLogger, StructuredAuditLogger>();

    }
}
