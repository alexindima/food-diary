using FluentValidation;
using FoodDiary.Application.Abstractions.Admin.Common;
using FoodDiary.Application.Abstractions.Authentication.Common;
using FoodDiary.Application.Abstractions.Authentication.Services;
using FoodDiary.Application.Identity.Authentication.Commands.BootstrapInitialAdmin;
using FoodDiary.Application.Identity.Authentication.Services;
using FoodDiary.Application.Identity.Email.Services;
using FoodDiary.Mediator;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Application.Identity;

public static class DependencyInjection {
    public static IServiceCollection AddIdentityModule(this IServiceCollection services) {
        services.AddFoodDiaryMediator(configuration =>
            configuration.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly));
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly, includeInternalTypes: true);

        services.AddScoped<IAuthenticationLoginEventCleanupService, AuthenticationLoginEventCleanupService>();
        services.AddScoped<IAuthenticationLoginEventReadService, AuthenticationLoginEventReadService>();
        services.AddScoped<IAuthenticationTokenService, AuthenticationTokenService>();
        services.AddScoped<IInitialAdminBootstrapService, InitialAdminBootstrapService>();
        services.AddScoped<IEmailSender, EmailSender>();
        services.AddScoped<IEmailTemplateAdministrationService, EmailTemplateAdministrationService>();
        services.AddScoped<IEmailTemplateAdministrationReadService, EmailTemplateAdministrationReadService>();

        return services;
    }
}
