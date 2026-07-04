using FoodDiary.Integrations.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Integrations;

public static partial class DependencyInjection {
    private static IServiceCollection AddIntegrationOptions(this IServiceCollection services, IConfiguration configuration) {
        services.AddOptions<S3Options>()
            .Bind(configuration.GetSection(S3Options.SectionName))
            .Validate(S3Options.HasValidMaxUploadSize,
                "S3:MaxUploadSizeBytes must be greater than zero.")
            .Validate(S3Options.HasValidPublicBaseUrl,
                "S3:PublicBaseUrl must be an absolute URL when provided.")
            .Validate(S3Options.HasValidServiceUrl,
                "S3:ServiceUrl must be an absolute URL when provided.")
            .ValidateOnStart();
        services.AddOptions<OpenAiOptions>()
            .Bind(configuration.GetSection(OpenAiOptions.SectionName))
            .Validate(OpenAiOptions.HasVisionFallbackWhenVisionModelConfigured,
                "OpenAi:VisionFallbackModel is required when VisionModel is configured.")
            .Validate(OpenAiOptions.HasTextModelWhenApiKeyConfigured,
                "OpenAi:TextModel is required when ApiKey is configured.")
            .Validate(OpenAiOptions.HasVisionModelWhenApiKeyConfigured,
                "OpenAi:VisionModel is required when ApiKey is configured.")
            .ValidateOnStart();
        services.AddOptions<GoogleAuthOptions>()
            .Bind(configuration.GetSection(GoogleAuthOptions.SectionName))
            .Validate(GoogleAuthOptions.HasValidClientId,
                "GoogleAuth:ClientId must be empty or a non-whitespace value.")
            .ValidateOnStart();
        services.AddOptions<TelegramAuthOptions>()
            .Bind(configuration.GetSection(TelegramAuthOptions.SectionName));
        services.AddOptions<BillingOptions>()
            .Bind(configuration.GetSection(BillingOptions.SectionName))
            .Validate(static options => !string.IsNullOrWhiteSpace(options.Provider),
                "Billing:Provider is required.")
            .Validate(static options => Domain.Entities.Billing.BillingProviderNames.IsSupported(options.Provider),
                "Billing:Provider must be a supported billing provider.")
            .ValidateOnStart();
        services.AddOptions<StripeOptions>()
            .Bind(configuration.GetSection(StripeOptions.SectionName))
            .Validate(static options =>
                    string.IsNullOrWhiteSpace(options.SecretKey) ||
                    (!string.IsNullOrWhiteSpace(options.WebhookSecret) &&
                     !string.IsNullOrWhiteSpace(options.PremiumMonthlyPriceId) &&
                     !string.IsNullOrWhiteSpace(options.PremiumYearlyPriceId) &&
                     Uri.IsWellFormedUriString(options.SuccessUrl, UriKind.Absolute) &&
                     Uri.IsWellFormedUriString(options.CancelUrl, UriKind.Absolute) &&
                     Uri.IsWellFormedUriString(options.PortalReturnUrl, UriKind.Absolute)),
                "Stripe configuration is incomplete.");
        services.AddOptions<PaddleOptions>()
            .Bind(configuration.GetSection(PaddleOptions.SectionName));
        services.AddOptions<YooKassaOptions>()
            .Bind(configuration.GetSection(YooKassaOptions.SectionName));
        services.AddOptions<WebPushOptions>()
            .Bind(configuration.GetSection(WebPushOptions.SectionName))
            .Validate(WebPushOptions.HasValidConfiguration,
                "WebPush configuration is invalid.")
            .ValidateOnStart();

        services.Configure<UsdaApiOptions>(configuration.GetSection(UsdaApiOptions.SectionName));
        services.Configure<OpenFoodFactsApiOptions>(configuration.GetSection(OpenFoodFactsApiOptions.SectionName));
        services.Configure<FitbitOptions>(configuration.GetSection(FitbitOptions.SectionName));
        services.Configure<GoogleFitOptions>(configuration.GetSection(GoogleFitOptions.SectionName));

        return services;
    }
}
