using FoodDiary.Integrations.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace FoodDiary.Integrations;

public static partial class DependencyInjection {
    private static void AddIntegrationOptions(this IServiceCollection services, IConfiguration configuration) {
        services.AddGeneralIntegrationOptions(configuration);
        services.AddBillingIntegrationOptions(configuration);
    }

    private static void AddGeneralIntegrationOptions(
        this IServiceCollection services,
        IConfiguration configuration) {
        services.AddOptions<S3Options>()
            .Bind(configuration.GetSection(S3Options.SectionName))
            .Validate(S3Options.IsEmptyOrComplete,
                "S3 configuration must be empty or include AccessKeyId, SecretAccessKey, distinct Bucket/StagingBucket values, and Region or ServiceUrl.")
            .Validate(S3Options.HasValidMaxUploadSize,
                "S3:MaxUploadSizeBytes must be greater than zero and no greater than 50 MiB.")
            .Validate(S3Options.HasValidPublicBaseUrl,
                "S3:PublicBaseUrl must be an absolute HTTP or HTTPS URL when provided.")
            .Validate(S3Options.HasExplicitPublicImageAccessPolicy,
                "S3:AllowPublicImageAccess must be true for configured storage because image URLs are shared with users and external AI providers.")
            .Validate(S3Options.HasValidServiceUrl,
                "S3:ServiceUrl must be an absolute HTTP or HTTPS URL when provided.")
            .ValidateOnStart();
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
    }

    private static void AddBillingIntegrationOptions(
        this IServiceCollection services,
        IConfiguration configuration) {
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
    }

    private static bool ShouldRequireProviderConfiguration(
        BillingOptions billingOptions,
        string provider,
        bool hasAnyProviderConfiguration) =>
        hasAnyProviderConfiguration ||
        (billingOptions.RequireConfiguredProvider &&
         string.Equals(billingOptions.Provider, provider, StringComparison.OrdinalIgnoreCase));
}
