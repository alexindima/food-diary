using FoodDiary.Application.Abstractions.Authentication.Abstractions;
using FoodDiary.Application.Abstractions.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace FoodDiary.Authentication.Infrastructure;

public static class AuthenticationInfrastructureRegistration {
    public static IServiceCollection AddSharedAuthentication(this IServiceCollection services, IConfiguration configuration) {
        services.TryAddSingleton(TimeProvider.System);
        services.TryAddSingleton<IAdminSsoCodeStore, InMemoryAdminSsoCodeStore>();
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
        return services;
    }
}
