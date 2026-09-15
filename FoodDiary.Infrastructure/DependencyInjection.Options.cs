using FoodDiary.Application.Abstractions.Options;
using FoodDiary.Application.Abstractions.Email.Common;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace FoodDiary.Infrastructure;

public static partial class DependencyInjection {
    private static void AddInfrastructureOptions(this IServiceCollection services, IConfiguration configuration) {
        services.AddOptions<JwtOptions>()
            .Bind(configuration.GetSection(JwtOptions.SectionName))
            .Validate(JwtOptions.HasValidSecretKey,
                $"{JwtOptions.SectionName}:SecretKey must be at least 32 characters long, have sufficient character diversity, and must not use a repository placeholder.")
            .Validate(static options => !string.IsNullOrWhiteSpace(options.Issuer),
                $"{JwtOptions.SectionName}:Issuer is required.")
            .Validate(static options => !string.IsNullOrWhiteSpace(options.Audience),
                $"{JwtOptions.SectionName}:Audience is required.")
            .Validate(static options => options.ExpirationMinutes > 0,
                $"{JwtOptions.SectionName}:ExpirationMinutes must be greater than zero.")
            .Validate(static options => options.RefreshTokenExpirationDays > 0,
                $"{JwtOptions.SectionName}:RefreshTokenExpirationDays must be greater than zero.")
            .Validate(static options => options.RememberMeRefreshTokenExpirationDays > 0,
                $"{JwtOptions.SectionName}:RememberMeRefreshTokenExpirationDays must be greater than zero.")
            .ValidateOnStart();

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

    }
}
