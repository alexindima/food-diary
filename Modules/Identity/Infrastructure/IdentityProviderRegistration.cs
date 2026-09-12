using FoodDiary.Application.Abstractions.Authentication.Abstractions;
using FoodDiary.Integrations.Authentication;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection.Extensions;
using FoodDiary.Integrations.Options;
using FoodDiary.Application.Abstractions.Authentication.Common;
using Microsoft.Extensions.Options;

namespace FoodDiary.Infrastructure;

public static class IdentityProviderRegistration {
    public static IServiceCollection AddIdentityProvider(this IServiceCollection services, IConfiguration configuration) {
        services.AddOptions<GoogleAuthOptions>()
            .Bind(configuration.GetSection(GoogleAuthOptions.SectionName))
            .Validate(GoogleAuthOptions.HasValidClientId,
                "GoogleAuth:ClientId must be empty or contain at most 512 non-whitespace characters.")
            .ValidateOnStart();
        services.AddOptions<TelegramAuthOptions>()
            .Bind(configuration.GetSection(TelegramAuthOptions.SectionName))
            .Validate(TelegramAuthOptions.HasValidAuthTtl,
                "TelegramAuth:AuthTtlSeconds must be greater than zero.")
            .ValidateOnStart();
        services.TryAddSingleton(TimeProvider.System);
        services.AddOptions<TelegramClientOptions>().Bind(configuration.GetSection(TelegramClientOptions.SectionName))
            .Validate<IOptions<TelegramAuthOptions>>((options, auth) => TelegramClientOptions.HasCompatibleBot(options, auth.Value),
                "Enabled Telegram features require a valid bot token; registration requires login, and operations BotId must match the token.")
            .Validate(options => !options.OperationsEnabled || options.BotId > 0,
                "Enabled Telegram operations require a positive bot ID.")
            .ValidateOnStart();
        services.AddSingleton<ITelegramIdentityPolicy>(provider => provider.GetRequiredService<IOptions<TelegramClientOptions>>().Value);
        services.AddSingleton<ITelegramOperationPolicy>(provider => provider.GetRequiredService<IOptions<TelegramClientOptions>>().Value);
        services.AddOptions<TelegramOidcOptions>()
            .Bind(configuration.GetSection(TelegramOidcOptions.SectionName))
            .Validate(TelegramOidcOptions.IsValid,
                "Enabled Telegram OIDC requires a positive client ID, client secret and an HTTPS callback URL.")
            .Validate<IOptions<TelegramAuthOptions>>((options, auth) => TelegramOidcOptions.HasCompatibleBot(options, auth.Value),
                "Enabled Telegram OIDC must use the same bot identity as Mini App authentication.")
            .ValidateOnStart();
        services.AddSingleton<ITelegramOidcTokenValidator, TelegramOidcTokenValidator>();
        services.AddHttpClient<ITelegramOidcProvider, TelegramOidcProvider>(client => client.Timeout = TimeSpan.FromSeconds(20))
            .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { AllowAutoRedirect = false });
        services.AddSingleton<IGoogleTokenValidator, GoogleTokenValidator>();
        services.AddSingleton<ITelegramAuthValidator, TelegramAuthValidator>();
        services.AddSingleton<ITelegramLoginWidgetValidator, TelegramLoginWidgetValidator>();
        return services;
    }
}
