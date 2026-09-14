using FoodDiary.Modules.Billing.Infrastructure.Providers.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace FoodDiary.Modules.Billing.Infrastructure.Providers;

public static partial class DependencyInjection {
    private static void AddBillingIntegrationOptions(
        this IServiceCollection services,
        IConfiguration configuration) {
        services.AddOptions<BillingOptions>()
            .Bind(configuration.GetSection(BillingOptions.SectionName))
            .Validate(static options => !string.IsNullOrWhiteSpace(options.Provider),
                "Billing:Provider is required.")
            .Validate(static options => global::FoodDiary.Modules.Billing.Domain.Contracts.BillingProviderNames.IsSupported(options.Provider),
                "Billing:Provider must be a supported billing provider.")
            .ValidateOnStart();
        services.AddOptions<StripeOptions>()
            .Bind(configuration.GetSection(StripeOptions.SectionName))
            .Validate<IOptions<BillingOptions>>(static (options, billingOptions) =>
                    !ShouldRequireProviderConfiguration(
                        billingOptions.Value,
                        global::FoodDiary.Modules.Billing.Domain.Contracts.BillingProviderNames.Stripe,
                        StripeOptions.HasAnyConfiguration(options)) ||
                    StripeOptions.HasValidConfiguration(options),
                "Stripe configuration is incomplete for the active billing provider.")
            .ValidateOnStart();
        services.AddOptions<PaddleOptions>()
            .Bind(configuration.GetSection(PaddleOptions.SectionName))
            .Validate<IOptions<BillingOptions>>(static (options, billingOptions) =>
                    !ShouldRequireProviderConfiguration(
                        billingOptions.Value,
                        global::FoodDiary.Modules.Billing.Domain.Contracts.BillingProviderNames.Paddle,
                        PaddleOptions.HasAnyConfiguration(options)) ||
                    PaddleOptions.HasValidConfiguration(options),
                "Paddle configuration is incomplete for the active billing provider.")
            .ValidateOnStart();
        services.AddOptions<YooKassaOptions>()
            .Bind(configuration.GetSection(YooKassaOptions.SectionName))
            .Validate<IOptions<BillingOptions>>(static (options, billingOptions) =>
                    !ShouldRequireProviderConfiguration(
                        billingOptions.Value,
                        global::FoodDiary.Modules.Billing.Domain.Contracts.BillingProviderNames.YooKassa,
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
