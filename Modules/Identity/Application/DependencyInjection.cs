using FoodDiary.Modules.Identity.Contracts.Authentication.Common;
using FoodDiary.Modules.Identity.Application.Abstractions.Authentication.Services;
using FluentValidation;
using FoodDiary.Modules.Identity.Application.Authentication.Services;
using FoodDiary.Mediator;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Modules.Identity.Application;

public static class DependencyInjection {
    public static IServiceCollection AddIdentityModule(this IServiceCollection services) {
        services.AddFoodDiaryMediator(configuration =>
            configuration.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly));
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly, includeInternalTypes: true);

        services.AddScoped<IAuthenticationTokenService, AuthenticationTokenService>();
        services.AddScoped<TelegramAuthenticationIntentService>();
        services.AddScoped<TelegramBackupEmailService>();
        services.AddScoped<IEmailSender, EmailSender>();

        return services;
    }
}
