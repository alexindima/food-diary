using FoodDiary.Integrations.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

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
            .Validate<IOptions<BillingOptions>>(static (options, billingOptions) =>
                    !ShouldRequireProviderConfiguration(
                        billingOptions.Value,
                        Domain.Entities.Billing.BillingProviderNames.Stripe,
                        StripeOptions.HasAnyConfiguration(options)) ||
                    StripeOptions.HasValidConfiguration(options),
                "Stripe configuration is incomplete for the active billing provider.")
            .ValidateOnStart();
        services.AddOptions<PaddleOptions>()
            .Bind(configuration.GetSection(PaddleOptions.SectionName))
            .Validate<IOptions<BillingOptions>>(static (options, billingOptions) =>
                    !ShouldRequireProviderConfiguration(
                        billingOptions.Value,
                        Domain.Entities.Billing.BillingProviderNames.Paddle,
                        PaddleOptions.HasAnyConfiguration(options)) ||
                    PaddleOptions.HasValidConfiguration(options),
                "Paddle configuration is incomplete for the active billing provider.")
            .ValidateOnStart();
        services.AddOptions<YooKassaOptions>()
            .Bind(configuration.GetSection(YooKassaOptions.SectionName))
            .Validate<IOptions<BillingOptions>>(static (options, billingOptions) =>
                    !ShouldRequireProviderConfiguration(
                        billingOptions.Value,
                        Domain.Entities.Billing.BillingProviderNames.YooKassa,
                        YooKassaOptions.HasAnyConfiguration(options)) ||
                    YooKassaOptions.HasValidCheckoutConfiguration(options),
                "YooKassa configuration is incomplete for the active billing provider.")
            .ValidateOnStart();
        services.AddOptions<WebPushOptions>()
            .Bind(configuration.GetSection(WebPushOptions.SectionName))
            .Validate(WebPushOptions.HasValidConfiguration,
                "WebPush configuration is invalid.")
            .ValidateOnStart();

        services.Configure<UsdaApiOptions>(configuration.GetSection(UsdaApiOptions.SectionName));
        services.Configure<OpenFoodFactsApiOptions>(configuration.GetSection(OpenFoodFactsApiOptions.SectionName));
        services.AddOptions<FitbitOptions>()
            .Bind(configuration.GetSection(FitbitOptions.SectionName))
            .Validate(FitbitOptions.IsEmptyOrComplete,
                "Fitbit configuration must be empty or include ClientId, ClientSecret, and an absolute RedirectUri.")
            .ValidateOnStart();
        services.AddOptions<GoogleFitOptions>()
            .Bind(configuration.GetSection(GoogleFitOptions.SectionName))
            .Validate(GoogleFitOptions.IsEmptyOrComplete,
                "GoogleFit configuration must be empty or include ClientId, ClientSecret, and an absolute RedirectUri.")
            .ValidateOnStart();

        return services;
    }

    private static bool ShouldRequireProviderConfiguration(
        BillingOptions billingOptions,
        string provider,
        bool hasAnyProviderConfiguration) =>
        hasAnyProviderConfiguration ||
        (billingOptions.RequireConfiguredProvider &&
         string.Equals(billingOptions.Provider, provider, StringComparison.OrdinalIgnoreCase));
}
