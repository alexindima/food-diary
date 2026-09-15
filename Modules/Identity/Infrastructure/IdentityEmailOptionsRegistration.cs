using FoodDiary.Application.Abstractions.Email.Common;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace FoodDiary.Modules.Identity.Infrastructure;

public static class IdentityEmailOptionsRegistration {
    public static IServiceCollection AddIdentityEmailOptions(this IServiceCollection services, IConfiguration configuration) {
        services.AddOptions<EmailOptions>()
            .Bind(configuration.GetSection(EmailOptions.SectionName))
            .Validate(EmailOptions.HasValidFrontendBaseUrl,
                "Email:FrontendBaseUrl must be an absolute HTTP(S) URL when provided.")
            .Validate(EmailOptions.HasValidAllowedFrontendBaseUrls,
                "Email:AllowedFrontendBaseUrls entries must be absolute HTTP(S) URLs.")
            .Validate(static options => !string.IsNullOrWhiteSpace(options.VerificationPath),
                "Email:VerificationPath is required.")
            .Validate(static options => !string.IsNullOrWhiteSpace(options.PasswordResetPath),
                "Email:PasswordResetPath is required.")
            .ValidateOnStart();
        services.AddSingleton(static sp => sp.GetRequiredService<IOptions<EmailOptions>>().Value);
        return services;
    }
}
