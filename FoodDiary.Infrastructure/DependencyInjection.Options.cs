using FoodDiary.Application.Abstractions.Email.Common;
using FoodDiary.Infrastructure.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace FoodDiary.Infrastructure;

public static partial class DependencyInjection {
    private static IServiceCollection AddInfrastructureOptions(this IServiceCollection services, IConfiguration configuration) {
        services.AddOptions<DatabaseOptions>()
            .Bind(configuration.GetSection(DatabaseOptions.SectionName))
            .Validate(static options => !options.EnableRetries || options.MaxRetryCount > 0,
                "Database:MaxRetryCount must be greater than zero when retries are enabled.")
            .Validate(static options => !options.EnableRetries || options.MaxRetryDelaySeconds > 0,
                "Database:MaxRetryDelaySeconds must be greater than zero when retries are enabled.")
            .ValidateOnStart();

        services.AddOptions<JwtOptions>()
            .Bind(configuration.GetSection(JwtOptions.SectionName))
            .Validate(JwtOptions.HasValidSecretKey,
                $"{JwtOptions.SectionName}:SecretKey must be at least 32 characters long and must not use a repository placeholder.")
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

        return services;
    }
}
